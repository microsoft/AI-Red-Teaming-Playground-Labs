// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Services.Ctfd;

public interface ICtfdFlagSubmissionService
{
    CtfdAuthApi? GetCtfdAuth();
    Task SubmitFlagAsync(string chatId);
    Task SubmitFlagAsync(string chatId, CtfdAuthApi ctfdAuth);
}
