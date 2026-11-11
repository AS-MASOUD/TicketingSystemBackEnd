namespace Ticketing.Tests;

// SQL integration cases share one restored database; role-management transactions must not overlap other fixtures.
[CollectionDefinition("Restored database", DisableParallelization = true)]
public sealed class RestoredDatabaseCollection { }
