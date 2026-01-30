using Motion.Desktop.Models.Mtp;
using Motion.Desktop.ViewModels.Editor;
using Xunit;

namespace Motion.Desktop.Tests.ViewModels;

public class TimelineEditorViewModelTests
{
    [Fact]
    public void SelectEvent_Logic_ShouldWorkCorrectly()
    {
        // Arrange
        var vm = new TimelineEditorViewModel();
        
        // Создаем два тестовых события
        var eventA = new TimelineEventViewModel(new MtpEvent { Id = "A" });
        var eventB = new TimelineEventViewModel(new MtpEvent { Id = "B" });

        // Act & Assert 1: Выбираем A
        vm.SelectEvent(eventA);

        Assert.Same(eventA, vm.SelectedEvent);       // Главное свойство указывает на A
        Assert.True(eventA.IsSelected);              // A подсвечено
        Assert.False(eventB.IsSelected);             // B не подсвечено

        // Act & Assert 2: Кликаем по B (Смена выделения)
        vm.SelectEvent(eventB);

        Assert.Same(eventB, vm.SelectedEvent);       // Теперь главное B
        Assert.False(eventA.IsSelected);             // A погасло!
        Assert.True(eventB.IsSelected);              // B загорелось

        // Act & Assert 3: Кликаем по B снова (Toggle off)
        vm.SelectEvent(eventB);

        Assert.Null(vm.SelectedEvent);               // Ничего не выбрано
        Assert.False(eventB.IsSelected);             // B погасло
        
        // Act & Assert 4: Выбираем B, а потом отправляем null (убрать выделение)
        vm.SelectEvent(eventB);

        Assert.NotNull(vm.SelectedEvent);
        Assert.False(eventA.IsSelected);
        Assert.True(eventB.IsSelected);             
        
        vm.SelectEvent(null);
        
        Assert.Null(vm.SelectedEvent);
        Assert.False(eventA.IsSelected);
        Assert.False(eventB.IsSelected);
        
    }
}