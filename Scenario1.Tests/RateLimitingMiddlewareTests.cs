using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scenario1.Tests
{
    public class RateLimitingMiddlewareTests
    {
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly Mock<IRateLimitService> _rateLimitServiceMock;

        public RateLimitingMiddlewareTests()
        {
            _nextMock = new Mock<RequestDelegate>();
            _rateLimitServiceMock = new Mock<IRateLimitService>();
        }

        [Fact]
        public async Task InvokeAsync_RequestAllowed_CallsNextMiddleware()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var bodyContent = "{ \"recipient\": \"user@example.com\", \"type\": \"status\" }";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));
            context.Request.ContentType = "application/json";
            context.Request.Path = "/api/notification/send";

            _rateLimitServiceMock.Setup(x => x.IsValidNotificationType("status")).Returns(true);
            _rateLimitServiceMock.Setup(x => x.GetRateLimitInfo("user@example.com", "status"))
                .ReturnsAsync(new RateLimitInfo
                {
                    Allowed = true,
                    Limit = 2,
                    Remaining = 1,
                    Reset = 60
                });

            var middleware = new RateLimitingMiddleware(_nextMock.Object, _rateLimitServiceMock.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _nextMock.Verify(x => x.Invoke(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_RequestNotAllowed_Returns429()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var bodyContent = "{ \"recipient\": \"user@example.com\", \"type\": \"status\" }";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));
            context.Request.ContentType = "application/json";
            context.Request.Path = "/api/notification/send";

            _rateLimitServiceMock.Setup(x => x.IsValidNotificationType("status")).Returns(true);
            _rateLimitServiceMock.Setup(x => x.GetRateLimitInfo("user@example.com", "status"))
                .ReturnsAsync(new RateLimitInfo
                {
                    Allowed = false,
                    Limit = 2,
                    Remaining = 0,
                    Reset = 30
                });

            var middleware = new RateLimitingMiddleware(_nextMock.Object, _rateLimitServiceMock.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
            _nextMock.Verify(x => x.Invoke(context), Times.Never);
        }
    }
}