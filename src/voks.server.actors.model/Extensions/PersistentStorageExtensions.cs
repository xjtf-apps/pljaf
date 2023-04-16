using Orleans.Runtime;

namespace voks.server.model;

internal static class PersistentStorageExtensions
{
    public static async Task SetValueAndPersistAsync<T>(this IPersistentState<T> state, T newValue)
    {
        state.State = newValue;
        await state.WriteStateAsync();
    }

    public static async Task AddItemAndPersistAsync<T>(this IPersistentState<List<T>> state, T newItem)
    {
        state.State.Add(newItem);
        await state.WriteStateAsync();
    }
    public static async Task RemoveItemAndPersistAsync<T>(this IPersistentState<List<T>> state, T itemToRemove)
    {
        state.State.Remove(itemToRemove);
        await state.WriteStateAsync();
    }
}