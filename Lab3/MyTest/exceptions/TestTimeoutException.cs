namespace MyTest.exceptions;

public class TestTimeoutException(int limit) : Exception($"Test execution timed out. Limit: {limit}ms");
