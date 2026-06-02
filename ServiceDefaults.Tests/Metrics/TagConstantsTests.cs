using FluentAssertions;
using ServiceDefaults.Metrics;
using Xunit;

namespace ServiceDefaults.Tests.Metrics;

public class TagConstantsTests
{
    // ── Keys ──────────────────────────────────────────────────────────────────

    [Fact] public void Keys_Operation_IsCorrect()  => TagConstants.Keys.Operation.Should().Be("operation");
    [Fact] public void Keys_Status_IsCorrect()     => TagConstants.Keys.Status.Should().Be("status");
    [Fact] public void Keys_ErrorType_IsCorrect()  => TagConstants.Keys.ErrorType.Should().Be("error.type");
    [Fact] public void Keys_Found_IsCorrect()      => TagConstants.Keys.Found.Should().Be("found");

    // ── Values ────────────────────────────────────────────────────────────────

    [Fact] public void Values_Success_IsCorrect()  => TagConstants.Values.Success.Should().Be("success");
    [Fact] public void Values_Failure_IsCorrect()  => TagConstants.Values.Failure.Should().Be("failure");
    [Fact] public void Values_Create_IsCorrect()   => TagConstants.Values.Create.Should().Be("create");
    [Fact] public void Values_Update_IsCorrect()   => TagConstants.Values.Update.Should().Be("update");
    [Fact] public void Values_Delete_IsCorrect()   => TagConstants.Values.Delete.Should().Be("delete");
    [Fact] public void Values_GetById_IsCorrect()  => TagConstants.Values.GetById.Should().Be("get_by_id");
    [Fact] public void Values_List_IsCorrect()     => TagConstants.Values.List.Should().Be("list");

    // ── Tags ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Tags_OperationList_HasCorrectKey()
        => TagConstants.Tags.OperationList.Key.Should().Be(TagConstants.Keys.Operation);

    [Fact]
    public void Tags_OperationList_HasCorrectValue()
        => TagConstants.Tags.OperationList.Value.Should().Be(TagConstants.Values.List);

    [Fact]
    public void Tags_OperationGetById_HasCorrectKey()
        => TagConstants.Tags.OperationGetById.Key.Should().Be(TagConstants.Keys.Operation);

    [Fact]
    public void Tags_OperationGetById_HasCorrectValue()
        => TagConstants.Tags.OperationGetById.Value.Should().Be(TagConstants.Values.GetById);

    [Fact]
    public void Tags_OperationCreate_HasCorrectValue()
        => TagConstants.Tags.OperationCreate.Value.Should().Be(TagConstants.Values.Create);

    [Fact]
    public void Tags_OperationUpdate_HasCorrectValue()
        => TagConstants.Tags.OperationUpdate.Value.Should().Be(TagConstants.Values.Update);

    [Fact]
    public void Tags_OperationDelete_HasCorrectValue()
        => TagConstants.Tags.OperationDelete.Value.Should().Be(TagConstants.Values.Delete);
}
