using Formarca.Domain.IdentityAccess;

namespace Formarca.Domain.Tests;

public class UserTableViewTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Activate_Keeps_Single_Active_View_Per_Table()
    {
        var compact = UserTableView.Create(
            UserId, TableViewCatalog.Contracts, "Compact", ["code", "brandName"], activate: true);
        var full = UserTableView.Create(
            UserId, TableViewCatalog.Contracts, "Full", ["code", "brandName", "status"], activate: true, [compact]);

        Assert.False(compact.IsActive);
        Assert.True(full.IsActive);
    }

    [Fact]
    public void Delete_Of_Active_View_Promotes_Another()
    {
        var older = UserTableView.Create(
            UserId, TableViewCatalog.Customers, "Older", ["name"], activate: false);
        var active = UserTableView.Create(
            UserId, TableViewCatalog.Customers, "Active", ["name", "email"], activate: true, [older]);

        active.Delete([older, active]);

        Assert.True(active.IsDeleted);
        Assert.False(active.IsActive);
        Assert.True(older.IsActive);
        Assert.False(older.IsDeleted);
    }

    [Fact]
    public void Delete_Of_Last_Active_View_Leaves_None()
    {
        var only = UserTableView.Create(
            UserId, TableViewCatalog.Brands, "Only", ["name"], activate: true);

        only.Delete([only]);

        Assert.True(only.IsDeleted);
        Assert.False(only.IsActive);
    }

    [Fact]
    public void Delete_Of_Inactive_View_Does_Not_Change_Active()
    {
        var active = UserTableView.Create(
            UserId, TableViewCatalog.Invoices, "Active", ["status"], activate: true);
        var spare = UserTableView.Create(
            UserId, TableViewCatalog.Invoices, "Spare", ["contractCode"], activate: false, [active]);

        spare.Delete([active, spare]);

        Assert.True(spare.IsDeleted);
        Assert.True(active.IsActive);
        Assert.False(active.IsDeleted);
    }

    [Fact]
    public void Create_Rejects_Invalid_And_Empty_Column_Keys_Json()
    {
        Assert.Throws<ArgumentException>(() =>
            TableViewCatalog.ParseColumnKeysJson("{bad", TableViewCatalog.Contracts));
        Assert.Throws<ArgumentException>(() =>
            TableViewCatalog.ParseColumnKeysJson("[]", TableViewCatalog.Contracts));
        Assert.Throws<ArgumentException>(() =>
            TableViewCatalog.ParseColumnKeysJson(" ", TableViewCatalog.Contracts));
    }

    [Fact]
    public void Unknown_Column_Keys_Are_Ignored()
    {
        var view = UserTableView.Create(
            UserId,
            TableViewCatalog.Contracts,
            "Mixed",
            ["code", "notARealColumn", "brandName", "ghost"],
            activate: false);

        Assert.Equal(["code", "brandName"], view.GetColumnKeys());
        Assert.Equal("""["code","brandName"]""", view.ColumnKeysJson);
    }

    [Fact]
    public void All_Unknown_Column_Keys_Are_Rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            UserTableView.Create(UserId, TableViewCatalog.Processes, "Bad", ["nope"], activate: false));
    }

    [Fact]
    public void Unknown_Table_Key_Is_Rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            UserTableView.Create(UserId, "not-a-table", "View", ["name"], activate: false));
    }

    [Fact]
    public void SetColumnKeys_Rejects_Empty_After_Filter()
    {
        var view = UserTableView.Create(
            UserId, TableViewCatalog.AdminUsers, "Users", ["email"], activate: false);

        Assert.Throws<ArgumentException>(() => view.SetColumnKeys(["unknown"]));
        Assert.Equal(["email"], view.GetColumnKeys());
    }
}
