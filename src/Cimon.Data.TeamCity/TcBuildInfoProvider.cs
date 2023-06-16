﻿using Cimon.Contracts;
using Cimon.Contracts.Services;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Locators;
using BuildLocator = TeamCitySharp.Locators.BuildLocator;
using BuildStatus = Cimon.Contracts.BuildStatus;
using CimonBuildLocator = Cimon.Contracts.BuildLocator;

namespace Cimon.Data.TeamCity;

public class TcBuildInfoProvider : IBuildInfoProvider
{

	
	private readonly TcClient _client;
	public TcBuildInfoProvider(TcClient client) {
		_client = client;
	}
	public CISystem CiSystem => CISystem.TeamCity;
	public async Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<CimonBuildLocator> locators) {
		return MockData.TestBuildInfos.Where(b => locators.Any(l=>l.Id == b.Name)).ToList();
		var builds = _client.GetBuilds();
		var allRunningBuilds = builds.AllRunningBuild();
		return locators.Select(l => {
			var locator = BuildLocator.WithDimensions(BuildTypeLocator.WithId(l.Id), maxResults:1);
			var build = builds.ByBuildLocator(locator).FirstOrDefault();
			if (build is null) return null;
			return new BuildInfo {
				Name = build.BuildConfig.Name,
				BuildId = l.Id,
				BuildHomeUrl = build.WebUrl,
				ProjectName = build.BuildConfig.Id,
				Number = build.Number,
				StatusText = build.StatusText,
				BranchName = build.BranchName,
				Committers = null,
				Status = GetStatus(build)
			};
		}).Where(x=>x is not null).ToList();
	}
	private BuildStatus GetStatus(Build build) {
		return build.Status.Equals("success", StringComparison.InvariantCultureIgnoreCase)
			? BuildStatus.Success
			: BuildStatus.Failed;
	}
}
