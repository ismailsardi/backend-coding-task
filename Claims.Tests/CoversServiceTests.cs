using Claims.Auditing;
using Claims.Premium;
using Claims.Repositories;
using Claims.Services;
using Claims.Validation;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Claims.Tests;

public class CoversServiceTests
{
    // "Today" for every test in this class
    private static readonly DateTime Today = new(2027, 1, 1);

    // Fake dependencies: they do nothing unless we tell them to
    private readonly ICoversRepository _repository = Substitute.For<ICoversRepository>();
    private readonly IAuditer _auditer = Substitute.For<IAuditer>();
    private readonly IPremiumCalculator _premiumCalculator = Substitute.For<IPremiumCalculator>();
    private readonly CoversService _service;

    // xUnit creates a new instance of this class for every test, so each test starts clean
    public CoversServiceTests()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(Today, TimeSpan.Zero));
        _service = new CoversService(_repository, _auditer, _premiumCalculator, timeProvider);
    }

    private static Cover NewCover(DateTime start, DateTime end) => new()
    {
        StartDate = start,
        EndDate = end,
        Type = CoverType.Yacht
    };

    [Fact]
    public async Task CreateAsync_StartDateInPast_Throws()
    {
        var cover = NewCover(Today.AddDays(-1), Today.AddDays(30));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(cover));

        // Nothing should be saved or audited when validation fails
        await _repository.DidNotReceive().AddAsync(Arg.Any<Cover>());
        _auditer.DidNotReceive().AuditCover(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_Throws()
    {
        var cover = NewCover(Today.AddDays(10), Today.AddDays(5));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(cover));
    }

    [Fact]
    public async Task CreateAsync_PeriodLongerThanOneYear_Throws()
    {
        var cover = NewCover(Today, Today.AddYears(1).AddDays(1));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(cover));
    }

    [Fact]
    public async Task CreateAsync_PeriodOfExactlyOneYear_IsAccepted()
    {
        var cover = NewCover(Today, Today.AddYears(1));

        var created = await _service.CreateAsync(cover);

        Assert.NotNull(created.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidCover_SetsPremiumSavesAndAudits()
    {
        var cover = NewCover(Today.AddDays(1), Today.AddDays(31));
        _premiumCalculator.Calculate(cover.StartDate, cover.EndDate, cover.Type).Returns(1000m);

        var created = await _service.CreateAsync(cover);

        Assert.Equal(1000m, created.Premium);
        await _repository.Received(1).AddAsync(cover);
        _auditer.Received(1).AuditCover(created.Id, "POST");
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalseAndDoesNotAudit()
    {
        _repository.DeleteAsync("unknown").Returns(false);

        var deleted = await _service.DeleteAsync("unknown");

        Assert.False(deleted);
        _auditer.DidNotReceive().AuditCover(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndAudits()
    {
        _repository.DeleteAsync("abc").Returns(true);

        var deleted = await _service.DeleteAsync("abc");

        Assert.True(deleted);
        _auditer.Received(1).AuditCover("abc", "DELETE");
    }
}