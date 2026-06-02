using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UjangKasir.Desktop.Helpers;
using UjangKasir.Desktop.Models;
using UjangKasir.Desktop.Services;

namespace UjangKasir.Desktop.ViewModels;

public partial class MemberViewModel : PageViewModelBase
{
    private readonly MemberService memberService;

    public ObservableCollection<Member> Members { get; } = new();
    public override string Title => "Member";
    public override string Description => "Kelola pelanggan tetap dan poin belanja.";

    [ObservableProperty]
    private Member selectedMember = new() { IsActive = true };

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isBusy;

    public MemberViewModel(MemberService memberService)
    {
        this.memberService = memberService;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var members = await memberService.SearchAsync(SearchText);
            Members.Clear();
            foreach (var member in members)
            {
                Members.Add(member);
            }

            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Load members");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void New()
    {
        SelectedMember = new Member { IsActive = true };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            await memberService.SaveAsync(SelectedMember);
            StatusMessage = "Member tersimpan.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Save member");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (SelectedMember.Id <= 0)
        {
            StatusMessage = "Pilih member terlebih dahulu.";
            return;
        }

        try
        {
            IsBusy = true;
            await memberService.DeactivateAsync(SelectedMember.Id);
            StatusMessage = "Member dinonaktifkan.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.Log(ex, "Deactivate member");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
