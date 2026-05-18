namespace Boj.Rackspace.UnitTest.Fixtures
{
    public static class RackSpaceTokenFixture
    {
        public static string SuccessResponse => """ { "access": { "token": { "id": "fake-access-token" } } } """;
        public static string UnauthorizedResponse => """ { "error": { "message": "Unauthorized" } } """;
    }
}
