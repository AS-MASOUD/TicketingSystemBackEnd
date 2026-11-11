namespace Ticketing.Domain.Enums;

public sealed record DeveloperOption(int Id, string Name);

public static class DeveloperCatalog
{
    public static IReadOnlyList<DeveloperOption> Active { get; } = Array.AsReadOnly(new[] {
        new DeveloperOption((int)Developer.s_mohamadzadeh, "ساناز محمدزاده"),
        new DeveloperOption((int)Developer.m_salehi, "امیرمسعود صالحی")
    });

    public static bool IsAssignable(Developer developer) =>
        developer is Developer.s_mohamadzadeh or Developer.m_salehi;
}
