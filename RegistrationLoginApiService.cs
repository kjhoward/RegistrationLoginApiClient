using DevConsulting.RegistrationLoginApi.Client;
using DevConsulting.RegistrationLoginApi.Client.Models;
using Newtonsoft.Json;
using JustHelpDesk.Models;
using Microsoft.AspNetCore.Http;
using DevConsulting.Common.Models.Extensions;
namespace DevConsulting.Client{

    public interface IRegistrationLoginApiService{
        public Task<MessageResponse> Authenticate(AuthenticateRequest loginForm);

        public Task<MessageResponse> Register(RegisterRequest registerForm);
        public Task<MessageResponse> SetUser();

        public void RemoveUser();

    }
    public class RegistrationLoginApiService : IRegistrationLoginApiService
    {
        private readonly HttpClient httpClient;

        private readonly IJwtUtils jwtUtils;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public RegistrationLoginApiService(HttpClient httpClient, IJwtUtils utils, IHttpContextAccessor httpContextAccessor)
        {
            this.httpClient = httpClient;
            jwtUtils = utils;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<MessageResponse> Authenticate(AuthenticateRequest loginForm)
        {
            var result = await httpClient.PostAsJsonAsync<AuthenticateRequest>("users/authenticate",loginForm);
            if (!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(response);

            //Set the Toke in the session
            UserSession.Token = authResponse.Token;

            return new MessageResponse{Message = $"Welcome back {authResponse.Username}", IsError=false};
        }

        public async Task<MessageResponse> Register(RegisterRequest registerForm)
        {
            var result = await httpClient.PostAsJsonAsync<RegisterRequest>("users/register",registerForm);
            if (!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            
            return new MessageResponse{Message = response, IsError=false};
        }

        public async Task<MessageResponse> SetUser()
        {

            var userId = jwtUtils.ValidateToken(UserSession.Token);
            if(!userId.HasValue)
                return new MessageResponse{Message = "Could not validate Token"};
            var result = await httpClient.AddTokenToHeader(UserSession.Token).GetAsync($"users/{userId.Value}");
            if (!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            var userResource =  JsonConvert.DeserializeObject<UserResource>(response);

            UserSession.UserInfo = userResource;

            //_httpContextAccessor.HttpContext.Response.Headers.Add("Authorization",UserSession.Token);

            return new MessageResponse{Message = "OK", IsError=false};
        }

        public void RemoveUser(){
            UserSession.UserInfo = null;
        }

        private async Task<MessageResponse> GetMessageFromFailedHttpResponse(HttpResponseMessage result){
            MessageResponse msg;
            try{
                var response = await result.Content.ReadAsStringAsync();
                msg = JsonConvert.DeserializeObject<MessageResponse>(response);
            }catch (Exception e){
                msg = new MessageResponse{Message = "Could not parse message"};
            }
            msg.IsError = true;
            return msg;
        }
    }
}