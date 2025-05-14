using System.ComponentModel;
using System.Threading.Tasks;

namespace Merq;

/// <summary>
/// Provides the <see cref="Forget"/> extension method to <see cref="ValueTask"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ValueTaskExtensions
{
    /// <summary>
    /// Observes the value task to avoid exceptions.
    /// </summary>
    public static void Forget(this ValueTask task)
    {
        // note: this code is inspired by a tweet from Ben Adams: https://twitter.com/ben_a_adams/status/1045060828700037125
        // Only care about tasks that may fault (not completed) or are faulted,
        // so fast-path for SuccessfullyCompleted and Canceled tasks.
        if (!task.IsCompleted || task.IsFaulted)
        {
            // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
            // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/discards?WT.mc_id=DT-MVP-5003978#a-standalone-discard
#pragma warning disable CA2012
            _ = ForgetAwaited(task);
#pragma warning restore CA2012
        }

        // Allocate the async/await state machine only when needed for performance reasons.
        // More info about the state machine: https://blogs.msdn.microsoft.com/seteplia/2017/11/30/dissecting-the-async-methods-in-c/?WT.mc_id=DT-MVP-5003978
        async static ValueTask ForgetAwaited(ValueTask task)
        {
            try
            {
                // No need to resume on the original SynchronizationContext, so use ConfigureAwait(false)
                await task.ConfigureAwait(false);
            }
            catch
            {
                // Nothing to do here
            }
        }
    }
}
