// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using Microsoft.Extensions.Configuration;

namespace Pocok.Modularity.Tests;

public sealed class ModuleContractTests
{
    [Test]
    public void ContextNormalizesDirectoryAndCopiesMetadata()
    {
        var metadata = new Dictionary<string, string> { ["supplier"] = "Acme" };
        var context = new ModuleContext(
            new ModuleIdentity("acme.codec", new Version(1, 2, 0)),
            ".",
            new ConfigurationBuilder().Build(),
            metadata);

        metadata["supplier"] = "Changed";

        Path.IsPathFullyQualified(context.BaseDirectory).ShouldBeTrue();
        context.Metadata["supplier"].ShouldBe("Acme");
        context.Identity.Id.ShouldBe("acme.codec");
    }

    [Test]
    public void InvalidIdentityAndContextInputsAreRejected()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        Should.Throw<ArgumentException>(() => new ModuleIdentity(" ", new Version(1, 0)));
        Should.Throw<ArgumentNullException>(() => new ModuleIdentity("module", null!));
        Should.Throw<ArgumentException>(() => new ModuleContext(
            new ModuleIdentity("module", new Version(1, 0)),
            " ",
            configuration));
    }
}
