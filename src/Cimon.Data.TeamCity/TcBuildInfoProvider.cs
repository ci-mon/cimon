﻿using Cimon.Contracts;
using Cimon.Contracts.Services;
using Microsoft.Extensions.Logging;
using TeamCitySharp;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Locators;
using BuildLocator = TeamCitySharp.Locators.BuildLocator;
using BuildStatus = Cimon.Contracts.BuildStatus;

namespace Cimon.Data.TeamCity;

public class TcBuildInfoProvider : IBuildInfoProvider
{

	private readonly ILogger _logger;
	private readonly TcClient _client;
	public TcBuildInfoProvider(TcClient client, ILogger<TcBuildInfoProvider> logger) {
		_client = client;
		_logger = logger;
	}
	public CISystem CiSystem => CISystem.TeamCity;

	public Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildConfigInfo> buildConfigs) {
		var builds = _client.GetBuilds();
		var allRunningBuilds = builds.AllRunningBuild();
		List<BuildInfo?> list = new List<BuildInfo?>();
		foreach (var l in buildConfigs) {
			var locator = BuildLocator.WithDimensions(BuildTypeLocator.WithId(l.Key), maxResults: 1);
			var build = builds.ByBuildLocator(locator).FirstOrDefault();
			if (build is null) return null;
			var info = new BuildInfo {
				Name = build.BuildConfig?.Name,
				BuildConfigId = l.Key,
				BuildHomeUrl = build.WebUrl,
				ProjectName = build.BuildConfig?.Id,
				Number = build.Number,
				StatusText = build.StatusText,
				BranchName = build.BranchName,
				Committers = null,
				Status = GetStatus(build)
			};
			list.Add(info);
		}
		return Task.FromResult((IReadOnlyCollection<BuildInfo>)list);
	}

	private BuildInfo Old(string buildConfigId, string branch, string[] tags = null) {
		TeamCityClient client = _client.CreateClient();
		BuildTypeLocator idLocator = BuildTypeLocator.WithId(buildConfigId);
		BuildConfig buildConfig = _client.GetBuildConfigs().ByConfigurationId(buildConfigId);
		BuildLocator locator = string.IsNullOrEmpty(branch) ? 
			BuildLocator.WithDimensions(idLocator, maxResults: 1, tags: tags) :
			BuildLocator.WithDimensions(idLocator, branch: branch, maxResults: 1, tags: tags);
		
		Build build = client.Builds.ByBuildLocator(locator).FirstOrDefault();
		if (buildConfig == null || build == null) {
			throw new BuildLoadException(string.Format("Build \"{0}\" can't be loaded", buildConfigId));
		}
		locator = BuildLocator.WithId(build.Id);
		build = client.Builds.DetailByBuildLocator(locator);
		string projectVersion = GetProjectVersion(buildConfig.Parameters);
		BuildStatus status = GetStatus(build);
		var clientBuild = new BuildInfo {
			Name = buildConfig.Name,
			Number = build.Number,
			ProjectName = projectVersion ?? buildConfig.Project.Name,
			Status = status,
			StatusText = build.StatusText,
			FinishDate = build.FinishDate,
			StartDate = build.StartDate,
			BranchName = build.BranchName,
			Committers = build.Commiters,
			BuildHomeUrl = build.WebUrl,
			BuildConfigId = buildConfigId
		};
		if (status == BuildStatus.Failed) {
			//StringCollection ignoreUsers = Settings.Default.IgnoreUsers;
			var ignoreUsers = new HashSet<string>();
			var lastModificationBy = new List<string>();
			string firstErrorBuildId = GetLastErrorBuildId(client, buildConfig.Id, branch);
			if (!string.IsNullOrEmpty(firstErrorBuildId)) {
				IEnumerable<Change> changes = client.Changes.ByBuildId(firstErrorBuildId);
				if (changes != null){
					foreach (Change change in changes) {
						if (lastModificationBy.Count >= 3) {
							break;
						}
						string userName = "undefined";
						try {
							Change changeDetail = client.Changes.ByChangeId(change.Id);
							userName = changeDetail.Username;
						} catch (Exception ex) {
							_logger.LogError(ex, "Couldn't load change ID = {ID}", change.Id);
						}
						if (!ignoreUsers.Contains(userName) && !lastModificationBy.Contains(userName)) {
							lastModificationBy.Add(userName);
						}
					}
				}
			}
			if (lastModificationBy.Count == 0) {
				lastModificationBy.Add("unknownuser");
			}
			clientBuild.LastModificationBy = lastModificationBy;
		}
		return clientBuild;
	}
	private string GetProjectVersion(Parameters parameters) {
		if (parameters == null) {
			return null;
		}
		Property projectVersion = parameters.Property.FirstOrDefault(prop => prop.Name == "Project.Version");
		if (projectVersion != null && !string.IsNullOrEmpty(projectVersion.Value)) {
			return projectVersion.Value;
		}
		return null;
	}
	private string GetLastErrorBuildId(TeamCityClient client, string buildConfigId, string branch) {
		BuildTypeLocator buildType = BuildTypeLocator.WithId(buildConfigId);
		var successLocator = string.IsNullOrEmpty(branch) ?
			BuildLocator.WithDimensions(buildType, status: TeamCitySharp.Locators.BuildStatus.SUCCESS) :
			BuildLocator.WithDimensions(buildType, status: TeamCitySharp.Locators.BuildStatus.FAILURE, branch: branch);
		List<Build> successBuilds = client.Builds.ByBuildLocator(successLocator);
		Build lastSuccessfullBuild = successBuilds.FirstOrDefault();
		BuildLocator locator = lastSuccessfullBuild != null ? successLocator : null;
		List<Build> failureBuilds = GetBuilds(client, buildConfigId, locator, branch);
		Build failureBuild = failureBuilds.OrderBy(build => ParseToInt(build.Id, int.MaxValue)).FirstOrDefault();
		return failureBuild != null ? failureBuild.Id : string.Empty;
	}
	
	private int ParseToInt(string stringNumber, int defValue) {
		if (!int.TryParse(stringNumber, out var number)) {
			number = defValue;
		}
		return number;
	}

	private List<Build> GetBuilds(TeamCityClient client, string id, BuildLocator sinceBuild, string branch) {
		BuildTypeLocator buildType = BuildTypeLocator.WithId(id);
		var locator = string.IsNullOrEmpty(branch) ?
			BuildLocator.WithDimensions(buildType, canceled: false, sinceBuild: sinceBuild):
			BuildLocator.WithDimensions(buildType, canceled: false, sinceBuild: sinceBuild, branch: branch);
		return client.Builds.ByBuildLocator(locator);
	}

	private BuildStatus GetStatus(Build build) {
		return build.Status.Equals("success", StringComparison.InvariantCultureIgnoreCase)
			? BuildStatus.Success
			: BuildStatus.Failed;
	}
}
