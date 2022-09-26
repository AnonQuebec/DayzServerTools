﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DayzServerTools.Application.ViewModels.Base;
using DayzServerTools.Application.Models;
using DayzServerTools.Application.Services;
using DayzServerTools.Application.Extensions;
using DayzServerTools.Library.Xml;

namespace DayzServerTools.Application.ViewModels;

public class ValidationErrorInfo
{
    public string Identifier { get; set; }
    public IEnumerable<string> Errors { get; set; }

    public ValidationErrorInfo(string identifier, IEnumerable<string> errors)
    {
        Identifier = identifier;
        Errors = errors;
    }
}

public partial class ItemTypesViewModel : ProjectFileViewModel<ItemTypes>, IDisposable
{

    [ObservableProperty]
    private ObservableCollection<ItemTypeViewModel> items = new();
    [ObservableProperty]
    private float quantityPercentage = 1;
    [ObservableProperty]
    private float lifetimePercentage = 1;
    [ObservableProperty]
    private float restockPercentage = 1;
    [ObservableProperty]
    private WorkspaceViewModel workspace = null;

    public ObservableCollection<ValidationErrorInfo> Errors { get; } = new();
    public IRelayCommand AddEmptyItemCommand { get; }
    public IRelayCommand<object> AdjustQuantityCommand { get; }
    public IRelayCommand<object> AdjustLifetimeCommand { get; }
    public IRelayCommand<object> AdjustRestockCommand { get; }
    public IRelayCommand<object> ExportToNewFileCommand { get; }
    public IRelayCommand<object> ExportToTraderCommand { get; }
    public IRelayCommand ValidateCommand { get; }

    public ItemTypesViewModel(IDialogFactory dialogFactory) : base(dialogFactory)
    {
        Model = new();
        Name = "types.xml";

        AddEmptyItemCommand = new RelayCommand(AddEmptyItem);
        AdjustQuantityCommand = new RelayCommand<object>(AdjustQuantity);
        AdjustLifetimeCommand = new RelayCommand<object>(AdjustLifetime);
        AdjustRestockCommand = new RelayCommand<object>(AdjustRestock);
        ExportToNewFileCommand = new RelayCommand<object>(ExportToNewFile);
        ExportToTraderCommand = new RelayCommand<object>(ExportToTrader);
        ValidateCommand = new RelayCommand(Validate);

        Items.CollectionChanged += ItemsCollectionChanged;
    }

    public void CopyItemTypes(IEnumerable<ItemType> source)
    {
        Items.AddRange(
            source.Select(obj => new ItemTypeViewModel(obj.Copy(), workspace))
        );
    }
    public void AddEmptyItem()
        => Items.Add(new(new ItemType(), Workspace));

    public void AdjustQuantity(object cmdParam)
    {
        var list = (System.Collections.IList)cmdParam;
        var viewModels = list.Cast<ItemTypeViewModel>();
        foreach (var item in viewModels)
        {
            item.AdjustQuantity(QuantityPercentage);
        }
        QuantityPercentage = 1;
    }
    public void AdjustLifetime(object cmdParam)
    {
        var list = (System.Collections.IList)cmdParam;
        var viewModels = list.Cast<ItemTypeViewModel>();
        foreach (var item in viewModels)
        {
            item.AdjustLifetime(LifetimePercentage);
        }
        LifetimePercentage = 1;
    }
    public void AdjustRestock(object cmdParam)
    {
        var list = (System.Collections.IList)cmdParam;
        var viewModels = list.Cast<ItemTypeViewModel>();
        foreach (var item in viewModels)
        {
            item.AdjustRestock(RestockPercentage);
        }
        RestockPercentage = 1;
    }
    public void Validate()
    {
        Errors.Clear();
        Items.AsParallel().ForAll(item => item.ValidateSelf());
        var allErrors = Items.AsParallel()
            .Where(item => item.HasErrors)
            .Select(item =>
                {
                    var errorMessages = item.GetErrors().Select(r => r.ErrorMessage);
                    return new ValidationErrorInfo(item.Name, errorMessages);
                }
            );
        Errors.AddRange(allErrors);
    }
    public void ExportToNewFile(object cmdParam)
    {
        var list = (System.Collections.IList)cmdParam;
        var viewModels = list.Cast<ItemTypeViewModel>();

        var items = viewModels.Select(vm => vm.Model);
        Workspace.CreateItemTypes(items);
    }
    public void ExportToTrader(object cmdParam)
    {
        var list = (System.Collections.IList)cmdParam;
        var viewModels = list.Cast<ItemTypeViewModel>();

        var items = viewModels.Select(vm => vm.Model);

        var dialog = _dialogFactory.CreateExportDialog();
        dialog.Items = items;
        dialog.ShowDialog();
    }


    protected override void OnLoad(Stream input, string filename)
    {
        var newItems = ItemTypes.ReadFromStream(input);
        Name = filename;
        Items.Clear();
        Items.AddRange(newItems.Types.Select(obj => new ItemTypeViewModel(obj, workspace)));
    }
    protected override IFileDialog CreateOpenFileDialog()
    {
        var dialog = _dialogFactory.CreateOpenFileDialog();
        dialog.FileName = "types*";
        return dialog;
    }
    protected override bool CanSave()
    {
        var isEmpty = Model is null || Model.Types.Count == 0;
        var isValid = !Items.Any(i => i.HasErrors);
        return !isEmpty && isValid;
    }

    private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var item in e.NewItems)
                {
                    Model.Types.Add(((ItemTypeViewModel)item).Model);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var item in e.OldItems)
                {
                    Model.Types.Remove(((ItemTypeViewModel)item).Model);
                }
                break;
            default:
                break;
        }
    }

    public void Dispose()
    {
        Items.CollectionChanged -= ItemsCollectionChanged;
    }
}
