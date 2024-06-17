namespace MO.MODBApi.Tests{
    [Collection("Integration")]
    public class SystemUsersTests
    {
        private readonly HttpClient _client;
        public SystemUsersTests(IntegrationFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public void Get()
        {

        }
    }
}