namespace MonopolyBank.Domain;

public class BankCard
{
    internal BankCard()
    {
    }

    public string Number { get; } = Guid.NewGuid().ToString();

    public DateTime Expiry { get; } = DateTime.Now.AddYears(1);

    public string Cvv { get; } = Random.Shared.Next(1000).ToString("D3");
}
