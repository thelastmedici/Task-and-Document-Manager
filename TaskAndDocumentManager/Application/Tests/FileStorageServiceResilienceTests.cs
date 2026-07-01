using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskAndDocumentManager.Infrastructure.Storage;

namespace TaskAndDocumentManager.Application.Tests.Infrastructure;

public class FileStorageServiceResilienceTests
{
    [Fact]
    public async Task SaveAsync_ShouldTimeoutAndDeletePartialFile_WhenStorageOperationHangs()
    {
        var userId = Guid.NewGuid();
        var userFolder = Path.Combine(AppContext.BaseDirectory, "storage", "uploads", userId.ToString());
        var sut = new FileStorageService(
            Options.Create(new FileStorageOptions
            {
                OperationTimeout = TimeSpan.FromMilliseconds(10)
            }),
            NullLogger<FileStorageService>.Instance);

        try
        {
            await using var content = new HangingReadStream();

            await Assert.ThrowsAsync<TimeoutException>(() =>
                sut.SaveAsync(userId, "report.pdf", content, CancellationToken.None));

            Assert.True(!Directory.Exists(userFolder) || !Directory.EnumerateFiles(userFolder).Any());
        }
        finally
        {
            if (Directory.Exists(userFolder))
            {
                Directory.Delete(userFolder, recursive: true);
            }
        }
    }

    private sealed class HangingReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
