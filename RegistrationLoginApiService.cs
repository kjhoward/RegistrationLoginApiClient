using DevConsulting.RegistrationLoginApi.Client;
using DevConsulting.RegistrationLoginApi.Client.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using DevConsulting.Common.Models.Extensions;
using DevConsulting.Common.Models;
namespace DevConsulting.Client{

    public interface IRegistrationLoginApiService{
        public Task<AuthenticateResponse?> Authenticate(AuthenticateRequest loginForm);

        public Task<MessageResponse> Register(RegisterRequest registerForm);
        public Task<MessageResponse> GetUser(long userId);
        public Task<MessageResponse> GetUser(string username);
        public Task<MessageResponse> Update(long id, UpdateRequest updateReq);
        public Task<MessageResponse> Delete(long id);

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
        public async Task<AuthenticateResponse?> Authenticate(AuthenticateRequest loginForm)
        {
            var result = await httpClient.PostAsJsonAsync<AuthenticateRequest>("users/authenticate",loginForm);
            if (!result.IsSuccessStatusCode)
                return null;

            var response = await result.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(response);
            if(authResponse == null)
                return null;
            //Set the Toke in the session
            UserSession.Token = authResponse.Token;

            return authResponse;
        }

        public async Task<MessageResponse> Register(RegisterRequest registerForm)
        {
            var result = await httpClient.PostAsJsonAsync<RegisterRequest>("users/register",registerForm);
            if (!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            
            return new MessageResponse{Message = response, IsError=false};
        }

        public async Task<MessageResponse> GetUser(long userId){
            var result = await httpClient.AddTokenToHeader(UserSession.Token).GetAsync($"users/{userId}");
            if(!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            return new MessageResponse {Message = response, IsError = false};
        }

        public async Task<MessageResponse> GetUser(string username){
            var result = await httpClient.AddTokenToHeader(UserSession.Token).GetAsync($"users/un/{username}");
            if(!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            return new MessageResponse {Message = response, IsError = false};
        }

        public async Task<MessageResponse> Update(long id, UpdateRequest updateReq){
            var result = await httpClient.AddTokenToHeader(UserSession.Token).PutAsJsonAsync<UpdateRequest>($"users/{id}",updateReq);
            if(!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            return new MessageResponse {Message = response, IsError = false};
        }

        public async Task<MessageResponse> Delete(long id){
            var result = await httpClient.AddTokenToHeader(UserSession.Token).DeleteAsync($"users/{id}");
            if(!result.IsSuccessStatusCode)
                return await GetMessageFromFailedHttpResponse(result);

            var response = await result.Content.ReadAsStringAsync();
            return new MessageResponse {Message = response, IsError = false};
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