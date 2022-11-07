﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentValidation;

using DayzServerTools.Application.ViewModels.Base;
using DayzServerTools.Application.Models;
using DayzServerTools.Application.Services;
using DayzServerTools.Application.Extensions;
using DayzServerTools.Application.Messages;
using DayzServerTools.Library.Trader;
using DayzServerTools.Library.Trader.Validators;

namespace DayzServerTools.Application.ViewModels.Trader;

public partial class TraderConfigViewModel : ProjectFileViewModel<TraderConfig>, IDisposable
{
    private readonly AbstractValidator<TraderConfig> _validator;
    [ObservableProperty]
    private TraderViewModel selectedTrader;

    public ObservableCollection<string> AvailableModifiers { get; }
    = new(new[] { "*", "W", "M", "V", "VNK", "K", "S" });
    public ObservableCollection<TraderViewModel> Traders { get; } = new();
    public CurrencyCategory CurrencyCategory
    {
        get => model.CurrencyCategory;
        set => SetProperty(model.CurrencyCategory, value, model, (m, n) => m.CurrencyCategory = n);
    }

    public IRelayCommand ValidateCommand { get; }

    public TraderConfigViewModel(IDialogFactory dialogFactory) : base(dialogFactory)
    {
        _validator = new TraderConfigValidator();
        Model = new();
        FileName = "TraderConfig.txt";
        ValidateCommand = new RelayCommand(Validate);

        Traders.CollectionChanged += TradersCollectionChanged;
    }
    
    protected override void OnLoad(Stream input, string filename)
    {
        var newModel = TraderConfig.ReadFromStream(input);
        CurrencyCategory = newModel.CurrencyCategory;
        Traders.AddRange(newModel.Traders.Select(t => new TraderViewModel(t)));
    }
    protected override IFileDialog CreateOpenFileDialog()
    {
        var dialog = _dialogFactory.CreateOpenFileDialog();
        dialog.FileName = "TraderConfig*";
        dialog.Filter = "Text|*.txt";
        return dialog;
    }
    protected override bool CanSave() => true;

    private void Validate()
    {
        WeakReferenceMessenger.Default.Send(new ClearValidationErrorsMessage(this));

        Traders.AsParallel()
          .Select(trader => new { trader.Name, Result = trader.ValidateSelf() })
          .Where(x => !x.Result.IsValid)
          .Select(x => new ValidationErrorInfo(this, x.Name, x.Result.Errors.Select(x => x.ErrorMessage)))
          .ForAll(error => WeakReferenceMessenger.Default.Send(error));

        var res = _validator.Validate(Model);
        if (!res.IsValid)
        {
            res.Errors.AsParallel()
                .Select(error => new ValidationErrorInfo(this, "", new[] { error.ErrorMessage }))
                .ForAll(error => WeakReferenceMessenger.Default.Send(error));
        }
    }
    private void TradersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var item in e.NewItems)
                {
                    Model.Traders.Add(((TraderViewModel)item).Model);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var item in e.OldItems)
                {
                    Model.Traders.Remove(((TraderViewModel)item).Model);
                }
                break;
            default:
                break;
        }
    }

    public void Dispose()
    {
        Traders.CollectionChanged -= TradersCollectionChanged;
    }
}
