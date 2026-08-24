using Formarca.Application.Common.Exceptions;
using Formarca.Application.IdentityAccess.TableViews.Commands;
using Formarca.Application.IdentityAccess.TableViews.Queries;
using Formarca.Domain.IdentityAccess;

namespace Formarca.Application.Tests;

public class UserTableViewHandlerTests
{
    [Fact]
    public async Task User_Cannot_Read_Or_Edit_Another_Users_Views()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var repository = new InMemoryRepository();

        var ownerView = UserTableView.Create(
            ownerId, TableViewCatalog.Contracts, "Mine", ["code"], activate: true);
        repository.Add(ownerView);

        var otherUser = new FakeCurrentUser { UserId = otherId };
        var list = await new ListUserTableViewsQueryHandler(otherUser, repository).Handle(
            new ListUserTableViewsQuery(TableViewCatalog.Contracts), CancellationToken.None);
        Assert.Empty(list);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateUserTableViewCommandHandler(otherUser, repository).Handle(
                new UpdateUserTableViewCommand(ownerView.Id, "Hacked", ["code", "brandName"]),
                CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ActivateUserTableViewCommandHandler(otherUser, repository).Handle(
                new ActivateUserTableViewCommand(ownerView.Id), CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteUserTableViewCommandHandler(otherUser, repository).Handle(
                new DeleteUserTableViewCommand(ownerView.Id), CancellationToken.None));

        Assert.True(ownerView.IsActive);
        Assert.False(ownerView.IsDeleted);
        Assert.Equal("Mine", ownerView.Name);
    }

    [Fact]
    public async Task Invalid_Table_Key_Fails()
    {
        var user = new FakeCurrentUser();
        var repository = new InMemoryRepository();

        var createResult = new CreateUserTableViewCommandValidator().Validate(
            new CreateUserTableViewCommand("not-a-table", "View", ["name"]));
        Assert.False(createResult.IsValid);
        Assert.Contains(createResult.Errors, e => e.PropertyName == nameof(CreateUserTableViewCommand.TableKey));

        var listResult = new ListUserTableViewsQueryValidator().Validate(new ListUserTableViewsQuery("nope"));
        Assert.False(listResult.IsValid);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new CreateUserTableViewCommandHandler(user, repository).Handle(
                new CreateUserTableViewCommand("not-a-table", "View", ["name"]),
                CancellationToken.None));

        await Assert.ThrowsAsync<ConflictException>(() =>
            new ListUserTableViewsQueryHandler(user, repository).Handle(
                new ListUserTableViewsQuery("not-a-table"), CancellationToken.None));
    }

    [Fact]
    public async Task Create_Activate_And_List_Are_Scoped_To_Current_User()
    {
        var userId = Guid.NewGuid();
        var user = new FakeCurrentUser { UserId = userId };
        var repository = new InMemoryRepository();
        var handler = new CreateUserTableViewCommandHandler(user, repository);

        var compact = await handler.Handle(
            new CreateUserTableViewCommand(TableViewCatalog.Customers, "Compact", ["name"], Activate: true),
            CancellationToken.None);
        var full = await handler.Handle(
            new CreateUserTableViewCommand(
                TableViewCatalog.Customers, "Full", ["name", "email", "status"], Activate: true),
            CancellationToken.None);

        Assert.True(full.IsActive);
        Assert.False(repository.Query<UserTableView>().Single(v => v.Id == compact.Id).IsActive);

        var listed = await new ListUserTableViewsQueryHandler(user, repository).Handle(
            new ListUserTableViewsQuery(TableViewCatalog.Customers), CancellationToken.None);

        Assert.Equal(2, listed.Count);
        Assert.Equal(full.Id, listed[0].Id);
        Assert.True(listed[0].IsActive);
        Assert.All(listed, item => Assert.Equal(TableViewCatalog.Customers, item.TableKey));
    }

    [Fact]
    public async Task Duplicate_Name_Conflicts()
    {
        var user = new FakeCurrentUser();
        var repository = new InMemoryRepository();
        var handler = new CreateUserTableViewCommandHandler(user, repository);

        await handler.Handle(
            new CreateUserTableViewCommand(TableViewCatalog.Brands, "Default", ["name"]),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateUserTableViewCommand(TableViewCatalog.Brands, "Default", ["name", "inpiClasses"]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_Ignores_Unknown_Column_Keys()
    {
        var user = new FakeCurrentUser();
        var handler = new CreateUserTableViewCommandHandler(user, new InMemoryRepository());

        var result = await handler.Handle(
            new CreateUserTableViewCommand(
                TableViewCatalog.Contracts, "Mixed", ["code", "unknown", "brandName"]),
            CancellationToken.None);

        Assert.Equal(["code", "brandName"], result.ColumnKeys);
    }
}
