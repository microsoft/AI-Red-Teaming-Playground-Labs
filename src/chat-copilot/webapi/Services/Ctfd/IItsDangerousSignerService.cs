// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Services.Ctfd;

public interface IItsDangerousSignerService
{
    public string Sign(string value);
}
