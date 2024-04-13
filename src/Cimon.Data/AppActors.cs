using System.Collections.Immutable;
using System.Reactive.Linq;
using Akka.Hosting;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.Data.Users;
using Cimon.DB.Models;
using Optional;
using User = Cimon.Contracts.User;

namespace Cimon.Data;

using System.Diagnostics;

public class AppActors
{
	public static Task<IObservable<IImmutableList<MentionInfo>>> GetMentions(User user,
			IRequiredActor<UserSupervisorActor> actor) {
		string name = user.Name.Name;
		bool nameIsEmpty = string.IsNullOrWhiteSpace(name);
		Debug.Assert(!nameIsEmpty, "nameIsEmpty", user?.ToString());
		if (nameIsEmpty) {
			return Task.FromResult(Observable.Empty<IImmutableList<MentionInfo>>());
		}
		return actor.ActorRef.Ask(new ActorsApi.GetUserMentions(name));
	}
}

public record MentionInBuildConfig(MentionInfo Mention, Option<BuildConfigModel> BuildConfig);
