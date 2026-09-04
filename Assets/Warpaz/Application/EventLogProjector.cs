using System;
using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Filters the event log for a viewer. Debug mode is opt-in and includes hidden events.
    /// </summary>
    public static class EventLogProjector
    {
        public static IReadOnlyList<GameEventView> Project(
            GameState state,
            PlayerId viewerId,
            MatchViewMode mode = MatchViewMode.Normal)
        {
            if (state == null)
            {
                return Array.Empty<GameEventView>();
            }

            var includeHidden = mode == MatchViewMode.Debug;
            var events = new List<GameEventView>(state.EventLog.Count);
            for (var i = 0; i < state.EventLog.Count; i++)
            {
                var evt = state.EventLog[i];
                if (!includeHidden && !evt.IsVisibleTo(viewerId))
                {
                    continue;
                }

                events.Add(GameEventView.From(evt));
            }

            return events;
        }
    }
}
