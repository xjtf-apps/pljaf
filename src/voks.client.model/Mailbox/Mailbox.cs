using System.Collections;

namespace voks.client.model;

public sealed class Mailbox : ICollection<Conversation>
{
    private readonly List<Conversation> conversations;

    public int Count => ((ICollection<Conversation>)conversations).Count;

    public bool IsReadOnly => ((ICollection<Conversation>)conversations).IsReadOnly;

    public Mailbox()
    {
        this.conversations = new List<Conversation>();
    }

    public Mailbox(IEnumerable<Conversation> conversations)
    {
        this.conversations = conversations.ToList();
    }

    public void Add(Conversation item)
    {
        ((ICollection<Conversation>)conversations).Add(item);
    }

    public void Clear()
    {
        ((ICollection<Conversation>)conversations).Clear();
    }

    public bool Contains(Conversation item)
    {
        return ((ICollection<Conversation>)conversations).Contains(item);
    }

    public void CopyTo(Conversation[] array, int arrayIndex)
    {
        ((ICollection<Conversation>)conversations).CopyTo(array, arrayIndex);
    }

    public IEnumerator<Conversation> GetEnumerator()
    {
        return ((IEnumerable<Conversation>)conversations).GetEnumerator();
    }

    public bool Remove(Conversation item)
    {
        return ((ICollection<Conversation>)conversations).Remove(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)conversations).GetEnumerator();
    }
}
