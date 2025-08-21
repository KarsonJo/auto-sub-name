using Moq;

namespace AutoSubName.Tests.Utils;

public interface ITestService<T>
    where T : class
{
    public Mock<T> Mock { get; }
}
