// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Services.Ctfd;

public class ItsDangerousSignerService : IItsDangerousSignerService
{
    private const string CtfdCookieSalt = "itsdangerous.Signersigner";
    private const string ItsDangerousSalt = "itsdangeroussigner";
    private const string Base64Padding = "=";

    private readonly ChallengeOptions _challengeOptions;
    private readonly byte[]? _key = null;

    public ItsDangerousSignerService(IOptions<ChallengeOptions> challengeOptions)
    {
        this._challengeOptions = challengeOptions.Value;

        string val = string.Empty;
        if (this._challengeOptions.Ctfd != null && !string.IsNullOrEmpty(this._challengeOptions.Ctfd.SecretKey))
        {
            val = CtfdCookieSalt + this._challengeOptions.Ctfd.SecretKey;
        }
        else if (this._challengeOptions.ChallengeHome != null && !string.IsNullOrEmpty(this._challengeOptions.ChallengeHome.SecretKey))
        {
            val = ItsDangerousSalt + this._challengeOptions.ChallengeHome.SecretKey;
        }

        if (!string.IsNullOrEmpty(val))
        {
#pragma warning disable CA5350 // Itsdangerous is using a Weak Cryptographic Algorithm
            this._key = SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(val));
#pragma warning restore CA5350
        }
    }

    public string Sign(string value)
    {
        if (this._key == null)
        {
            throw new ArgumentException("The Secret Key was not set");
        }

#pragma warning disable CA5350 // Ctfd is using a Weak Cryptographic Algorithm
        using var hmac = new HMACSHA1(this._key);
#pragma warning restore CA5350
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));

        var base64 = Convert.ToBase64String(hash);
        return base64.TrimEnd(Base64Padding.ToCharArray()).Replace('+', '-').Replace('/', '_');
    }
}
