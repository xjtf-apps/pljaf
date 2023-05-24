using System.Text;

namespace voks.server.api;

public class WebSocketClientAuthorization
{
    private readonly string Secret = "aBCFV33129_Ix56X";
    private readonly TimeSpan Expiration = TimeSpan.FromSeconds(15);

    public string IssueTicket()
    {
        var now = DateTime.UtcNow.Ticks;
        var nowBytes = BitConverter.GetBytes(now);
        var secretBytes = Encoding.UTF8.GetBytes(Secret);
        var payloadBytes = nowBytes.Concat(secretBytes).ToArray();

        var carrier = new byte[payloadBytes.Length * 2];
        var carrierRng = new Random();
        carrierRng.NextBytes(carrier);

        for (int i = 0; i < payloadBytes.Length; i++)
        {
            carrier[i * 2] = payloadBytes[i];
        }
        var value = Encoding.UTF8.GetString(carrier);
        var ticket = value.Reverse().ToString();
        return ticket!;
    }

    public bool VerifyTicket(string? ticket)
    {
        if (ticket == null) return false;

        var reversed = ticket.Reverse().ToString();
        var value = Encoding.UTF8.GetBytes(reversed!);
        var payloadBytes = new byte[value.Length / 2];

        for (int i = 0; i < value.Length; i++)
        {
            if (i == 0 || i % 2 == 0)
            {
                payloadBytes[i == 0 ? 0 : i / 2] = value[i];
            }
        }

        var now = DateTime.UtcNow;
        var secretBytes = payloadBytes[8..];
        var timestampBytes = payloadBytes[..8];
        var secretValue = Encoding.UTF8.GetString(secretBytes);
        var timestampValue = BitConverter.ToInt64(timestampBytes, 0);
        var timestamp = DateTime.FromBinary(timestampValue);

        var secretMatches = secretValue == Secret;
        var expired = timestamp.Add(Expiration) < now;
        return secretMatches && !expired;
    }
}
