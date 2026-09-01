using System;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class MigrationsTest
{
    [Test]
    public void TestDataDatabaseMigrations()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var success = Migrator.Migrate(connection, "SS14.Launcher.Models.Data.Migrations");
        Assert.That(success, Is.True, "Data DB migrations failed");

        // Verify SchemaVersions table exists and has entries
        var versionCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SchemaVersions");
        Assert.That(versionCount, Is.GreaterThan(0));

        // Verify key tables were created
        var tableCount = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('CVar', 'FavoriteServer', 'Login', 'ServerFilter', 'Hub')");
        Assert.That(tableCount, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void TestContentManagementDatabaseMigrations()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var success = Migrator.Migrate(connection, "SS14.Launcher.Models.ContentManagement.Migrations");
        Assert.That(success, Is.True, "Content management migrations failed");

        var versionCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SchemaVersions");
        Assert.That(versionCount, Is.GreaterThan(0));

        var tableCount = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('ContentVersion', 'ContentBlob', 'ContentManifest')");
        Assert.That(tableCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void TestOverrideAssetsDatabaseMigrations()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var success = Migrator.Migrate(connection, "SS14.Launcher.Models.OverrideAssets.Migrations");
        Assert.That(success, Is.True, "Override assets migrations failed");

        var versionCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SchemaVersions");
        Assert.That(versionCount, Is.GreaterThan(0));
    }

    [Test]
    public void TestIdempotentMigrations()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var first = Migrator.Migrate(connection, "SS14.Launcher.Models.Data.Migrations");
        Assert.That(first, Is.True);
        var countFirst = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SchemaVersions");

        // Second run should apply 0 additional migrations and succeed
        var second = Migrator.Migrate(connection, "SS14.Launcher.Models.Data.Migrations");
        Assert.That(second, Is.True);
        var countSecond = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SchemaVersions");

        Assert.That(countSecond, Is.EqualTo(countFirst));
    }
}
