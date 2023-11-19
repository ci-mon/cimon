using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Discussions;

namespace Cimon.Data.Actors;

public class DiscussionActor : ReceiveActor
{
	private readonly BuildDiscussionState _state = new();

	public DiscussionActor() {
		//await _mediator.Publish(new DiscussionClosedNotification(discussion));
		Receive<BuildInfo>(info => {
			if (_state.Status == BuildDiscussionStatus.Unknown) {
				_state.Status = BuildDiscussionStatus.Open;
				// add comments
				//await _mediator.Publish(new DiscussionOpenNotification(discussion));
			}
			Context.Parent.Tell(_state);
		});
		
	}
}
