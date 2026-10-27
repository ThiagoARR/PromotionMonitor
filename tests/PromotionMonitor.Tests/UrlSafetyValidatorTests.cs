using FluentAssertions;
using PromotionMonitor.Application.Services;

namespace PromotionMonitor.Tests;

public class UrlSafetyValidatorTests
{
    private readonly UrlSafetyValidator _sut = new();

    [Theory]
    [InlineData("https://example.com/product/1")]
    [InlineData("http://shop.example.org/x")]
    public void Allows_public_http_urls(string url)
    {
        _sut.Validate(url).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ftp://example.com/a")]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://localhost/admin")]
    [InlineData("http://127.0.0.1/x")]
    [InlineData("http://10.0.0.5/x")]
    [InlineData("http://192.168.1.1/x")]
    [InlineData("http://172.16.0.1/x")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://metadata.google.internal/")]
    [InlineData("http://service.local/x")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void Blocks_ssrf_and_invalid(string url)
    {
        var result = _sut.Validate(url);
        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
