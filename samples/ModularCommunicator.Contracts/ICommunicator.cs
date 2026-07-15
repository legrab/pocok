// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

namespace Pocok.Samples.ModularCommunicator;

public interface ICommunicator
{
    public string Id { get; }

    public string Send(string request);
}
