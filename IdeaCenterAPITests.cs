using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using IdeaCenterAPITests.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace IdeaCenterAPITests
{
    public class IdeaCenterAPITests : IDisposable
    {
        private RestClient client;
        private static readonly string BaseUrl = Environment.GetEnvironmentVariable("API_BASEURL");
        private static readonly string Email = Environment.GetEnvironmentVariable("USER_EMAIL");
        private static readonly string Password = Environment.GetEnvironmentVariable("USER_PASSWORD");
        private static string lastIdeaId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetAccessToken(Email, Password);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetAccessToken(string email, string password)
        {
            var authClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication");
            request.AddJsonBody(new
            {
                email = Email,
                password = Password
            });

            var response = authClient.Execute(request, Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.That(response.Content,Is.Not.Null,"Response content is not as expected");

                var responseBody = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var accessToken = responseBody.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new InvalidOperationException("Access Token is null or empty");
                }

                return accessToken;
            }
            else
            {
                throw new InvalidOperationException($"Authentication failed with {response.StatusCode} and {response.Content} message");
            }
        }

        [Order(1)]
        [Test]
        public void IdeaCenterAPITests_CreateNewIdea_WithAllRequiredFields_ShouldSucceed()
        {

            // Arrange
            string name = "New Idea";
            string description = "New Idea description";
            string expectedMessage = "Successfully created!";
            var newIdea = new IdeaDTO
            {
                Title = name,
                Description = description
            };
            var createRequest = new RestRequest("/api/Idea/Create");
            createRequest.AddJsonBody(newIdea);

            // Act
            var createResponse = client.Execute(createRequest, Method.Post);


            // Assert
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Status code is not as expected");

            Assert.That(createResponse.Content, Is.Not.Null, "Response content is not as expected");

            var createdIdea = JsonSerializer.Deserialize<ApiResponseDTO>(createResponse.Content);

            Assert.That(createdIdea, Is.Not.Null);

            Assert.That(createdIdea.Msg, Is.EqualTo(expectedMessage), "Response message is not as expected");
        }

        [Order(2)]
        [Test]
        public void IdeaCenterAPITests_GetAllIdeas_ShouldReturnNonEmptyArray()
        {
            // Arrange

            var getAllRequest = new RestRequest("/api/Idea/All");

            // Act
            var getAllResponse = client.Execute(getAllRequest, Method.Get);


            // Assert
            Assert.That(getAllResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Status code is not as expected");

            Assert.That(getAllResponse.Content, Is.Not.Null, "Response content is not as expected");

            var allIdeas = JsonSerializer.Deserialize<ApiResponseDTO[]>(getAllResponse.Content);

            Assert.That(allIdeas, Is.Not.Null);

            Assert.That(allIdeas.Length, Is.GreaterThan(0), "Listed items are less than one");

            lastIdeaId = allIdeas[allIdeas.Length - 1].IdeaId;

            Assert.That(lastIdeaId, Is.Not.Null);
        }

        [Order(3)]
        [Test]
        public void IdeaCenterAPITests_EditLastCreatedIdea_WithValidDate_ShouldSucceed()
        {

            // Arrange
            string name = "Edited Idea";
            string description = "Other Idea description";
            string expectedMessage = "Edited successfully";
            var Idea = new IdeaDTO
            {
                Title = name,
                Description = description
            };
            var updateRequest = new RestRequest("/api/Idea/Edit");
            updateRequest.AddQueryParameter("ideaId", lastIdeaId);
            updateRequest.AddJsonBody(Idea);

            // Act
            var updateResponse = client.Execute(updateRequest, Method.Put);


            // Assert
            Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Status code is not as expected");

            Assert.That(updateResponse.Content, Is.Not.Null, "Response content is not as expected");

            var updatedIdea = JsonSerializer.Deserialize<ApiResponseDTO>(updateResponse.Content);

            Assert.That(updatedIdea,Is.Not.Null);

            Assert.That(updatedIdea.Msg, Is.EqualTo(expectedMessage), "Response message is not as expected");
        }

        [Order(4)]
        [Test]
        public void IdeaCenterAPITests_RemoveLastCreatedIdea_ShouldSucceed()
        {

            // Arrange
            string expectedMessage = "The idea is deleted!";

            var deleteRequest = new RestRequest("/api/Idea/Delete");
            deleteRequest.AddQueryParameter("ideaId", lastIdeaId);

            // Act
            var deleteResponse = client.Execute(deleteRequest, Method.Delete);


            // Assert
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Status code is not as expected");

            Assert.That(deleteResponse.Content, Does.Contain(expectedMessage), "Response message is not as expected");
        }

        [Order(5)]
        [Test]
        public void IdeaCenterAPITests_CreateNewIdea_WithInvalidData_ShouldFail()
        {

            // Arrange
            string name = "New Idea";

            var newIdea = new IdeaDTO
            {
                Title = name
            };

            var createRequest = new RestRequest("/api/Idea/Create");
            createRequest.AddJsonBody(newIdea);

            // Act
            var createResponse = client.Execute(createRequest, Method.Post);


            // Assert
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Status code is not as expected");
        }

        [Order(6)]
        [Test]
        public void IdeaCenterAPITests_EditNonExistingIdea_WithValidDate_ShoulFail()
        {

            // Arrange
            string name = "Edited Idea";
            string description = "Other Idea description";
            string expectedMessage = "There is no such idea!";
            string fakeIdeaId = "123456";
            var Idea = new IdeaDTO
            {
                Title = name,
                Description = description
            };
            var updateRequest = new RestRequest("/api/Idea/Edit");
            updateRequest.AddQueryParameter("ideaId", fakeIdeaId);
            updateRequest.AddJsonBody(Idea);

            // Act
            var updateResponse = client.Execute(updateRequest, Method.Put);


            // Assert
            Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Status code is not as expected");

            Assert.That(updateResponse.Content, Does.Contain(expectedMessage), "Response message is not as expected");
        }

        [Order(7)]
        [Test]
        public void IdeaCenterAPITests_RemoveNonExistingIdea_ShouldFail()
        {

            // Arrange
            string fakeIdeaId = "123456";
            string expectedMessage = "There is no such idea!";

            var deleteRequest = new RestRequest("/api/Idea/Delete");
            deleteRequest.AddQueryParameter("ideaId", fakeIdeaId);

            // Act
            var deleteResponse = client.Execute(deleteRequest, Method.Delete);


            // Assert
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Status code is not as expected");

            Assert.That(deleteResponse.Content, Does.Contain(expectedMessage), "Response message is not as expected");
        }
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
