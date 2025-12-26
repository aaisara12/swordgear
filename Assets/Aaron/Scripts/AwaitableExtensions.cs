#nullable enable

using System.Threading.Tasks;
using UnityEngine;

public static class AwaitableExtensions
{
    private static async Awaitable AsAwaitable(this AsyncOperation asyncOperation)
    {
        await asyncOperation;
    }
    
    public static async Task AsTask(this AsyncOperation asyncOperation)
    {
        await asyncOperation.AsAwaitable();
    }
}
