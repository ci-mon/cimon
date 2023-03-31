﻿namespace Cimon.Data.TeamCity;

using System.Text.Json;
using System.Text.Json.Serialization;

public class TcBuildInfoProvider:IBuildInfoProvider
{
	private int counter;

	public CISystem CiSystem => CISystem.TeamCity;

	public Task<IList<BuildInfo>> GetInfo(IEnumerable<BuildLocator> locators) {
		/*var buildInfos = locators.Select(l => new BuildInfo {
			BuildId = l.Id,
			Name = $"build {l.Id} {++counter}"
		}).ToList();*/
		var buildInfos = JsonSerializer.Deserialize<BuildInfo[]>("[{\"BuildHomeUrl\":\"https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=BpmsPlatformWorkDiagnostic&tab=buildTypeStatusDiv\",\"ProjectName\":\"Team Diagnostics\",\"Name\":\"BpmsPlatformWorkDiagnostic\",\"Number\":\"8.1.0.0\",\"StatusText\":\"Tests passed: 339, ignored: 10, muted: 2\",\"Status\":0,\"FinishDate\":\"2023-03-31T20:02:41+03:00\",\"StartDate\":\"2023-03-31T18:27:36+03:00\",\"BranchName\":\"trunk\",\"Commiters\":\"\",\"LastModificationBy\":[],\"GetFinishDateString\":\"31.03.2023 20:02:41\",\"GetStartDateString\":\"18:27\"},{\"BuildHomeUrl\":\"https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv\",\"ProjectName\":\"Core\",\"Name\":\"Unit\",\"Number\":\"8.1.0.0 \",\"StatusText\":\"Tests passed: 23760, ignored: 31, muted: 3\",\"Status\":0,\"FinishDate\":\"2023-03-31T20:59:50+03:00\",\"StartDate\":\"2023-03-31T20:36:28+03:00\",\"BranchName\":\"trunk\",\"Commiters\":\"\",\"LastModificationBy\":[],\"GetFinishDateString\":\"31.03.2023 20:59:50\",\"GetStartDateString\":\"20:36\"},{\"BuildHomeUrl\":\"https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=NetCoreAppUnitTests&tab=buildTypeStatusDiv\",\"ProjectName\":\"Core\",\"Name\":\"Unit (.Net Core 3.1)\",\"Number\":\"8.1.0.0 \",\"StatusText\":\"Tests passed: 21358, ignored: 373, muted: 4\",\"Status\":0,\"FinishDate\":\"2023-03-31T21:29:51+03:00\",\"StartDate\":\"2023-03-31T21:03:35+03:00\",\"BranchName\":\"trunk\",\"Commiters\":\"\",\"LastModificationBy\":[],\"GetFinishDateString\":\"31.03.2023 21:29:51\",\"GetStartDateString\":\"21:03\"},{\"BuildHomeUrl\":\"https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=Team_CoreCraft_Custom_Net6_UnitNet6&tab=buildTypeStatusDiv\",\"ProjectName\":\"Core\",\"Name\":\"Unit (.Net 6)\",\"Number\":\"8.1.0.0 \",\"StatusText\":\"Tests passed: 21343, ignored: 384, muted: 4\",\"Status\":0,\"FinishDate\":\"2023-03-31T21:42:20+03:00\",\"StartDate\":\"2023-03-31T21:23:25+03:00\",\"BranchName\":\"trunk\",\"Commiters\":\"\",\"LastModificationBy\":[],\"GetFinishDateString\":\"31.03.2023 21:42:20\",\"GetStartDateString\":\"21:23\"},{\"BuildHomeUrl\":\"https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=UnitTestIntegration780&tab=buildTypeStatusDiv\",\"ProjectName\":\"Core\",\"Name\":\"Integration (MSSQL)\",\"Number\":\"8.1.0.610 ProductBase Softkey ENU\",\"StatusText\":\"Tests passed: 1723, ignored: 30, muted: 4\",\"Status\":0,\"FinishDate\":\"2023-03-31T21:17:46+03:00\",\"StartDate\":\"2023-03-31T20:43:54+03:00\",\"BranchName\":\"trunk\",\"Commiters\":\"\",\"LastModificationBy\":[],\"GetFinishDateString\":\"31.03.2023 21:17:46\",\"GetStartDateString\":\"20:43\"},{\"BuildHomeUrl\":\"https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=IntegrationPostgreSQL&tab=buildTypeStatusDiv\",\"ProjectName\":\"Core\",\"Name\":\"Integration (PostgreSQL)\",\"Number\":\"8.1.0.601 ProductBase Softkey ENU\",\"StatusText\":\"Tests passed: 2212, ignored: 31, muted: 3\",\"Status\":0,\"FinishDate\":\"2023-03-31T19:43:39+03:00\",\"StartDate\":\"2023-03-31T19:13:17+03:00\",\"BranchName\":\"trunk\",\"Commiters\":\"\",\"LastModificationBy\":[],\"GetFinishDateString\":\"31.03.2023 19:43:39\",\"GetStartDateString\":\"19:13\"},{\"BuildHomeUrl\":\"https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=DotNetOracleIntegrationTests&tab=buildTypeStatusDiv\",\"ProjectName\":\"Core\",\"Name\":\"Integration (Oracle)\",\"Number\":\"8.1.0.601 ProductBase Softkey ENU\",\"StatusText\":\"Tests passed: 1710, ignored: 55, muted: 7\",\"Status\":0,\"FinishDate\":\"2023-03-31T20:07:33+03:00\",\"StartDate\":\"2023-03-31T19:14:52+03:00\",\"BranchName\":\"trunk\",\"Commiters\":\"\",\"LastModificationBy\":[],\"GetFinishDateString\":\"31.03.2023 20:07:33\",\"GetStartDateString\":\"19:14\"},{\"BuildHomeUrl\":\"https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.shell/job/master/4850/\",\"ProjectName\":null,\"Name\":\"app.studio-enterprise.shell\",\"Number\":null,\"StatusText\":null,\"Status\":1,\"FinishDate\":\"0001-01-01T00:00:00\",\"StartDate\":\"0001-01-01T00:00:00\",\"BranchName\":null,\"Commiters\":\"Alexandr Kravchuk,\",\"LastModificationBy\":[\"Alexandr Kravchuk\"],\"GetFinishDateString\":\"01.01.0001 00:00:00\",\"GetStartDateString\":\"00:00\"},{\"BuildHomeUrl\":\"https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.schema-view/job/master/9248/\",\"ProjectName\":null,\"Name\":\"app.studio-enterprise.schema-view\",\"Number\":null,\"StatusText\":null,\"Status\":1,\"FinishDate\":\"0001-01-01T00:00:00\",\"StartDate\":\"0001-01-01T00:00:00\",\"BranchName\":null,\"Commiters\":\"Alexandr Kravchuk,\",\"LastModificationBy\":[\"Alexandr Kravchuk\"],\"GetFinishDateString\":\"01.01.0001 00:00:00\",\"GetStartDateString\":\"00:00\"},{\"BuildHomeUrl\":\"https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.process-designer/job/master/7727/\",\"ProjectName\":null,\"Name\":\"app.studio-enterprise.process-designer\",\"Number\":null,\"StatusText\":null,\"Status\":0,\"FinishDate\":\"0001-01-01T00:00:00\",\"StartDate\":\"0001-01-01T00:00:00\",\"BranchName\":null,\"Commiters\":\"Maxym Ocheretianko,Alexandr Kravchuk,Maksim Bazhenov,BPMonlineBuild,\",\"LastModificationBy\":[\"Maxym Ocheretianko\",\"Alexandr Kravchuk\",\"Maksim Bazhenov\"],\"GetFinishDateString\":\"01.01.0001 00:00:00\",\"GetStartDateString\":\"00:00\"},{\"BuildHomeUrl\":\"https://ts1-infr-jenkins.bpmonline.com/job/lib.studio-enterprise.process/job/master/257/\",\"ProjectName\":null,\"Name\":\"lib.studio-enterprise.process\",\"Number\":null,\"StatusText\":null,\"Status\":0,\"FinishDate\":\"0001-01-01T00:00:00\",\"StartDate\":\"0001-01-01T00:00:00\",\"BranchName\":null,\"Commiters\":\"Evgeniy Zinchenko,Maxim Zubok,Sergey Gulevatiy,Alexey Gorobets,Alexey Gorobets,Vladimir Mishin,Alexandr Kravchuk,Sergey Gulevatiy,Evgenii Sorokotiaga,Igor Agaev,Alexandr Kravchuk,Dmitrii Baranovskii,Dmitrii Baranovskii,Evgenii Sorokotiaga,Evgenii Sorokotiaga,Alexandr Kravchuk,Evgenii Sorokotiaga,Alexey Gorobets,Evgenii Sorokotiaga,Evgenii Sorokotiaga,Evgeniy Slapik,Alexandr Kravchuk,Sergey Tkachenko,Dmitrii Tokarev,Alexandr Kravchuk,Sergey Tkachenko,Alexandr Dudka,Alexandr Dudka,Evgenii Sorokotiaga,Evgenii Sorokotiaga,Evgeniy Slapik,Evgenii Sorokotiaga,Vladimir Mishin,Maksim Bazhenov,Vadim Bratskyi,Sergey Tkachenko,Alexandr Dudka,Sergey Tkachenko,Vadim Bratskyi,Vadim Bratskyi,Igor Agaev,Sergey Alekseenko,Vladimir Mishin,Evgenii Sorokotiaga,Vladimir Mishin,Aleksandr Pashchenko,Evgeniy Zinchenko,Dmitriy Nagayko,Sergey Alekseenko,Dmitriy Nagayko,Alexandr Kravchuk,\",\"LastModificationBy\":[\"Evgeniy Zinchenko\",\"Maxim Zubok\",\"Sergey Gulevatiy\",\"Alexey Gorobets\",\"Alexey Gorobets\",\"Vladimir Mishin\",\"Alexandr Kravchuk\",\"Sergey Gulevatiy\",\"Evgenii Sorokotiaga\",\"Igor Agaev\",\"Alexandr Kravchuk\",\"Dmitrii Baranovskii\",\"Dmitrii Baranovskii\",\"Evgenii Sorokotiaga\",\"Evgenii Sorokotiaga\",\"Alexandr Kravchuk\",\"Evgenii Sorokotiaga\",\"Alexey Gorobets\",\"Evgenii Sorokotiaga\",\"Evgenii Sorokotiaga\",\"Evgeniy Slapik\",\"Alexandr Kravchuk\",\"Sergey Tkachenko\",\"Dmitrii Tokarev\",\"Alexandr Kravchuk\",\"Sergey Tkachenko\",\"Alexandr Dudka\",\"Alexandr Dudka\",\"Evgenii Sorokotiaga\",\"Evgenii Sorokotiaga\",\"Evgeniy Slapik\",\"Evgenii Sorokotiaga\",\"Vladimir Mishin\",\"Maksim Bazhenov\",\"Vadim Bratskyi\",\"Sergey Tkachenko\",\"Alexandr Dudka\",\"Sergey Tkachenko\",\"Vadim Bratskyi\",\"Vadim Bratskyi\",\"Igor Agaev\",\"Sergey Alekseenko\",\"Vladimir Mishin\",\"Evgenii Sorokotiaga\",\"Vladimir Mishin\",\"Aleksandr Pashchenko\",\"Evgeniy Zinchenko\",\"Dmitriy Nagayko\",\"Sergey Alekseenko\",\"Dmitriy Nagayko\",\"Alexandr Kravchuk\"],\"GetFinishDateString\":\"01.01.0001 00:00:00\",\"GetStartDateString\":\"00:00\"}]");
		foreach (var info in buildInfos) {
			info.BuildId = locators.First().Id;
		}
		return Task.FromResult((IList<BuildInfo>)buildInfos);
	}
}