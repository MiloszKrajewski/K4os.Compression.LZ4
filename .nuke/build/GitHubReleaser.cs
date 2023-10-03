using System;
using System.IO;
using System.Threading.Tasks;
using NuGet.Versioning;
using Nuke.Common.ChangeLog;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Octokit;
using Octokit.Internal;
using Serilog;

public class GitHubReleaser
{
	static async Task UploadReleaseAssetToGithub(Release release, string asset)
	{
		await using var artifactStream = File.OpenRead(asset);
		var fileName = Path.GetFileName(asset);
		var assetUpload = new ReleaseAssetUpload {
			FileName = fileName,
			ContentType = "application/octet-stream",
			RawData = artifactStream,
		};
		Log.Information("Uploading {FileName}...", fileName);
		await GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, assetUpload);
	}

	public static async Task Release(
		string token,
		NuGetVersion packageVersion,
		GitRepository gitRepository,
		GitVersion gitVersion,
		ReleaseNotes releaseNotes,
		AbsolutePath[] artifacts)
	{
		var credentials = new Credentials(token);

		GitHubTasks.GitHubClient = new GitHubClient(
			new ProductHeaderValue(nameof(GitHubReleaser)),
			new InMemoryCredentialStore(credentials));

		var releaseTag = packageVersion.ToString();
		var repositoryOwner = gitRepository.GetGitHubOwner();
		var repositoryName = gitRepository.GetGitHubName();

		Log.Information("Creating draft release {ReleaseTag}...", releaseTag);

		var newRelease = new NewRelease(releaseTag) {
			TargetCommitish = gitVersion.Sha,
			Draft = true,
			Name = $"v{releaseTag}",
			Prerelease = packageVersion.IsPrerelease,
			Body = releaseNotes.Notes.Join("\n"),
		};

		var createdRelease = await GitHubTasks
			.GitHubClient
			.Repository
			.Release.Create(repositoryOwner, repositoryName, newRelease);

		foreach (var artifact in artifacts)
			await UploadReleaseAssetToGithub(createdRelease, artifact);

		Log.Information("Publishing release {ReleaseTag}...", releaseTag);
		await GitHubTasks
			.GitHubClient
			.Repository
			.Release
			.Edit(
				repositoryOwner, repositoryName, createdRelease.Id,
				new ReleaseUpdate { Draft = false });
	}
}
