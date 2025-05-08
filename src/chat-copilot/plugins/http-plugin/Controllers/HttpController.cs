// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;


namespace HttpPlugin.Controllers;
[Route("api/[controller]")]
[ApiController]
public class HttpController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpController(IHttpClientFactory httpClientFactory)
    {
        this._httpClientFactory = httpClientFactory;
    }


    /// <summary>
    /// Make an HTTP GET request to the specified URI.
    /// </summary>
    /// <param name="uriParam">Url of the resources to make a GET request to</param>
    /// <returns></returns>
    /// <response code="200">Returns the response from the uriParam</response>
    [HttpGet(Name = "HttpGet")]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<string>> Get([FromQuery] string uriParam)
    {
        try
        {
            var uri = new Uri(uriParam);
            var ip = Dns.GetHostAddresses(uri.Host)[0];
            if (ip.IsPrivate())
            {
                return this.BadRequest("The uri points to an internal host. The plugin will not call this uri.");
            }

            using var httpClient = this._httpClientFactory.CreateClient("httpClient");

            var response = await httpClient.GetAsync(uri);
            try
            {
                response.EnsureSuccessStatusCode();
                return this.Ok(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException e)
            {
                return this.BadRequest($"The plugin encountered an invalid status code. Status code: {e.StatusCode}");
            }
        }
        catch (UriFormatException)
        {
            return this.BadRequest("The URI is invalid. This plugin expects a valid URI.");
        }
        catch (SocketException e)
        {
            return this.BadRequest($"The plugin encountered an error: {e.Message}");
        }

    }
}
