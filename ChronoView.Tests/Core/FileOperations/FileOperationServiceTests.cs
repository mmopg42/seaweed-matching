using ChronoView.Core.FileOperations;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileOperations;

public class FileOperationServiceTests
{
    [Fact]
    public async Task MoveFileGroupAsync_DelegatesToMoveService()
    {
        var move = new Mock<IMoveService>();
        var del = new Mock<IDeleteService>();
        var logger = new Mock<ILogger<FileOperationService>>();

        move.Setup(m => m.BatchMoveAsync(
                It.IsAny<IEnumerable<FileGroup>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<IProgress<OperationProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult { Success = true });

        var svc = new FileOperationService(move.Object, del.Object, logger.Object);
        var group = new FileGroup { GroupId = "G1" };

        var res = await svc.MoveFileGroupAsync(group, destinationPath: "C:\\dest");

        Assert.True(res.Success);
        move.Verify(m => m.BatchMoveAsync(
            It.Is<IEnumerable<FileGroup>>(gs => gs.Single().GroupId == "G1"),
            "C:\\dest",
            1,
            1,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFileGroupAsync_DelegatesToDeleteService()
    {
        var move = new Mock<IMoveService>();
        var del = new Mock<IDeleteService>();
        var logger = new Mock<ILogger<FileOperationService>>();

        del.Setup(d => d.DeleteGroupAsync(
                It.IsAny<FileGroup>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IProgress<OperationProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult { Success = true });

        var svc = new FileOperationService(move.Object, del.Object, logger.Object);
        var group = new FileGroup { GroupId = "G2" };

        var res = await svc.DeleteFileGroupAsync(group, quarantinePath: "C:\\q");

        Assert.True(res.Success);
        del.Verify(d => d.DeleteGroupAsync(group, "C:\\q", null, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}




