using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;


namespace Foody
{
    [TestFixture]
    public class FoodyTests
    {
        private RestClient client;
        private static string createdFoodId;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("KolBoi", "KolKol1");

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]

        public void CreateFood_ShouldReturnCreated()
        {
            var food = new
            {
                Name = "New food",
                Description = "Vkusno brat",
                Url = ""
            };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;

            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food Id should no tbe empty");

        }

        [Test, Order(2)]

        public void EditFoodTitle_ShouldReturnOk()
        {
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "Updated"}
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);

            request.AddJsonBody(changes);

            var response = client.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);



            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]

        public void GetAllFoods_ShouldReturnList()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);

            Assert.That(foods, Is.Not.Empty);
        }

        [Test, Order(4)]

        public void DeleteFood_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Test, Order(5)]

        public void CreateFoodWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var food = new
            {
                Name = "",
                Description = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]

        public void EditFoodThatDoesNotExist_ShouldReturnNotFound()
        {
            string fakeId = "123";
            var changes = new[]
            {
                new { path = "/name", op = "replace", value = "New Title"}
            };

            var request = new RestRequest($"/api/Food/Edit/{fakeId}", Method.Patch);
            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            Assert.That(response.Content, Does.Contain("No food revues..."));
        }

        [Test, Order(7)]

        public void DeleteFoodThatDoesNotExist_ShouldReturnBadRequest()
        {
            string fakeId = "123";

            var request = new RestRequest($"/api/Food/Delete/{fakeId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));

        }


        [OneTimeTearDown]
        public void CleanUp()
        {
            client?.Dispose();
        }
    }
}