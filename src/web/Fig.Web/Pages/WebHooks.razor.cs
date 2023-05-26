using Fig.Common.NetStandard.WebHook;
using Fig.Web.Facades;
using Fig.Web.Models.WebHooks;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Namotion.Reflection;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class WebHooks
{
    [Inject]
    private IWebHookFacade WebHookFacade { get; set; } = null!;
    
    private RadzenDataGrid<WebHookClientModel> _webHookClientsGrid = null!;

    private List<WebHookClientModel> WebHookClients => WebHookFacade.WebHookClients;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;

    private List<WebHookTypeEnumerable> WebHookTypes { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        foreach (var item in Enum.GetValues(typeof(WebHookType)))
        {
            WebHookTypes.Add(new WebHookTypeEnumerable { EnumName = item.ToString()!, EnumValue = (WebHookType)item });
        }
        
        
        await WebHookFacade.LoadAllClients();
        await WebHookFacade.LoadAllWebHooks();
        await base.OnInitializedAsync();
        await _webHookClientsGrid.Reload();
    }
    
    private async Task AddClient()
    {
        var newModel = new WebHookClientModel();
        WebHookClients.Add(newModel);
        await _webHookClientsGrid.Reload();
        await _webHookClientsGrid.EditRow(newModel);
    }

    private async Task EditRow(WebHookClientModel row)
    {
        row.Snapshot();
        await _webHookClientsGrid.EditRow(row);
    }

    private async Task SaveRow(WebHookClientModel row)
    {
        var error = row.Validate(WebHookClients);
        if (error is not null)
        {
            await ShowCloseableFromOverlayDialog(error);
            return;
        }
        
        if (!await AddOrUpdateClient(row))
            return;
        
        await _webHookClientsGrid.UpdateRow(row);
    }

    private async Task CancelEdit(WebHookClientModel row)
    {
        row.Revert();
        _webHookClientsGrid.CancelEditRow(row);
        if (row.Id is null)
        {
            WebHookClients.Remove(row);
            await _webHookClientsGrid.Reload();
        }
    }
    
    private async Task<bool> AddOrUpdateClient(WebHookClientModel client)
    {
        if (SecretRequiresHashing())
        {
            NotificationService.Notify(NotificationFactory.Info("Processing...",
                "Hashing secret, this takes a few seconds"));
            await Task.Run(client.HashSecret);
        }
        
        if (client.Id is null)
            try
            {
                await WebHookFacade.AddClient(client);
                NotificationService.Notify(NotificationFactory.Success("Success",
                    $"Web Hook Client {client.Name} successfully created."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Add Client Failed", e.Message));
                return false;
            }
        else
            try
            {
                await WebHookFacade.SaveClient(client);
                NotificationService.Notify(NotificationFactory.Success("Success",
                    $"Web Hook Client {client.Name} successfully updated."));
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Update Web Hook Client Failed", e.Message));
                return false;
            }

        return true;

        bool SecretRequiresHashing()
        {
            return !string.IsNullOrWhiteSpace(client.Secret);
        }
    }

    private async Task DeleteRow(WebHookClientModel row)
    {
        if (!await GetDeleteConfirmation(row.Name ?? "<UNKNOWN>"))
            return;
        
        try
        {
            await WebHookFacade.DeleteClient(row);
            await _webHookClientsGrid.Reload();
            if (row.Id is not null)
                NotificationService.Notify(NotificationFactory.Success("Success",
                    $"Web Hook Client {row.Name} successfully removed."));
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationFactory.Failure("Failed", $"Unable to delete Web Hook Client. {e.Message}"));
        }
    }

    private void AddWebHook()
    {
        WebHookFacade.WebHooks.Add(new WebHookModel() { IsInEditMode = true });
    }

    private class WebHookTypeEnumerable
    {
        public WebHookType EnumValue { get; set; }
        
        public string EnumName { get; set; }
    }

    private async Task SaveWebHook(WebHookModel webHook)
    {
        var error = webHook.Validate();
        if (!string.IsNullOrEmpty(error))
        {
            NotificationService.Notify(NotificationFactory.Failure("Unable to save web hook", error));
            return;
        }

        await WebHookFacade.SaveWebHook(webHook);
        NotificationService.Notify(NotificationFactory.Success("Saved", "Web hook saved successfully."));
        webHook.IsInEditMode = false;
    }

    private async Task DeleteWebHook(WebHookModel webHook)
    {
        if (!await GetDeleteConfirmation("the webhook"))
            return;

        await WebHookFacade.DeleteWebHook(webHook);
    }
}