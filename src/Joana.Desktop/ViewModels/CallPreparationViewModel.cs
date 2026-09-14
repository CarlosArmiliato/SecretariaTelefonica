using System.ComponentModel;
using System.Windows.Input;
using Joana.Application.Orchestration;
using Joana.Domain.Calls;
using Joana.Domain.Primitives;

namespace Joana.Desktop.ViewModels;

/// <summary>Simulation-only operator state for preflight review and final dial confirmation.</summary>
public sealed class CallPreparationViewModel : INotifyPropertyChanged
{
    private readonly PreflightCoordinator? preflightCoordinator;
    private readonly CallAttemptCoordinator? callAttemptCoordinator;
    private readonly CallAttempt? attempt;
    private readonly SemaphoreSlim commandGate = new(1, 1);
    private CallRequest? request;
    private PreflightRevision? revision;
    private DialAuthorization? authorization;
    private bool simulationMode;
    private bool supervisionPresent;
    private bool confirmationOpen;
    private bool confirmationConsumed;
    private bool confirmationInvalidated;
    private bool stateUncertain;
    private bool secondInstanceDetected;
    private bool storageFailure;
    private string confirmationCompany = string.Empty;
    private string confirmationPhoneNumber = string.Empty;
    private string confirmationObjective = string.Empty;
    private OperationResult lastResult = OperationResult.Fail("NotStarted");

    public CallPreparationViewModel()
        : this(null, null, null, null, simulationMode: true, supervisionPresent: false)
    {
    }

    public CallPreparationViewModel(
        PreflightCoordinator? preflightCoordinator,
        CallAttemptCoordinator? callAttemptCoordinator,
        CallAttempt? attempt,
        CallRequest? request,
        bool simulationMode = true,
        bool supervisionPresent = false)
    {
        this.preflightCoordinator = preflightCoordinator;
        this.callAttemptCoordinator = callAttemptCoordinator;
        this.attempt = attempt;
        this.request = request?.Snapshot();
        this.simulationMode = simulationMode;
        this.supervisionPresent = supervisionPresent;

        ReviewPreflightCommand = new AsyncCommand(() => ReviewPreflightAsync(), () => ReviewEnabled);
        RequestDialConfirmationCommand = new AsyncCommand(() => RequestDialConfirmationAsync(), () => CanRequestDialConfirmation);
        ConfirmAndDialCommand = new AsyncCommand(() => ConfirmAndDialAsync(), () => CanConfirmAndDial);
        CancelDialCommand = new AsyncCommand(() => CancelDialAsync(), () => ConfirmationOpen);
        RefreshState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The editable preflight. It is copied before a revision is created.</summary>
    public CallRequest? Request
    {
        get => request;
        set
        {
            var next = value?.Snapshot();
            if (RequestsEqual(request, next)) return;

            request = next;
            if (revision is not null && !RequestsEqual(revision.Request, request))
            {
                InvalidatePendingConfirmation("PreflightChanged");
                SetLastResult(OperationResult.Fail("PreflightChanged"));
            }

            OnPropertyChanged();
            RefreshState();
        }
    }

    public CallRequest? Preflight
    {
        get => Request;
        set => Request = value;
    }

    public CallAttempt? Attempt => attempt;
    public PreflightRevision? Revision => revision;
    public DialAuthorization? Authorization => authorization;

    public bool SimulationMode
    {
        get => simulationMode;
        set
        {
            if (simulationMode == value) return;
            simulationMode = value;
            if (!value) InvalidatePendingConfirmation("SimulationOnly");
            OnPropertyChanged();
            RefreshState();
        }
    }

    public bool SupervisionPresent
    {
        get => supervisionPresent;
        set
        {
            if (supervisionPresent == value) return;
            supervisionPresent = value;
            if (!value) InvalidatePendingConfirmation("SupervisionRequired");
            OnPropertyChanged();
            RefreshState();
        }
    }

    public bool ReviewEnabled { get; private set; }
    public bool CanRequestDialConfirmation { get; private set; }
    public bool CanConfirmAndDial { get; private set; }

    public bool ConfirmationOpen
    {
        get => confirmationOpen;
        private set
        {
            if (confirmationOpen == value) return;
            confirmationOpen = value;
            OnPropertyChanged();
        }
    }

    public bool ConfirmationConsumed
    {
        get => confirmationConsumed;
        private set
        {
            if (confirmationConsumed == value) return;
            confirmationConsumed = value;
            OnPropertyChanged();
        }
    }

    public bool ConfirmationInvalidated
    {
        get => confirmationInvalidated;
        private set
        {
            if (confirmationInvalidated == value) return;
            confirmationInvalidated = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Failure-closed flags supplied by the host after persistence checks.</summary>
    public bool StateUncertain
    {
        get => stateUncertain;
        set
        {
            if (stateUncertain == value) return;
            stateUncertain = value;
            OnPropertyChanged();
            RefreshState();
        }
    }

    public bool SecondInstanceDetected
    {
        get => secondInstanceDetected;
        set
        {
            if (secondInstanceDetected == value) return;
            secondInstanceDetected = value;
            OnPropertyChanged();
            RefreshState();
        }
    }

    public bool StorageFailure
    {
        get => storageFailure;
        set
        {
            if (storageFailure == value) return;
            storageFailure = value;
            OnPropertyChanged();
            RefreshState();
        }
    }

    public string ConfirmationCompany
    {
        get => confirmationCompany;
        private set
        {
            if (confirmationCompany == value) return;
            confirmationCompany = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowsCompanyNumberAndObjective));
        }
    }

    public string ConfirmationPhoneNumber
    {
        get => confirmationPhoneNumber;
        private set
        {
            if (confirmationPhoneNumber == value) return;
            confirmationPhoneNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowsCompanyNumberAndObjective));
        }
    }

    public string ConfirmationObjective
    {
        get => confirmationObjective;
        private set
        {
            if (confirmationObjective == value) return;
            confirmationObjective = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowsCompanyNumberAndObjective));
        }
    }

    public bool ShowsCompanyNumberAndObjective =>
        ConfirmationOpen &&
        !string.IsNullOrWhiteSpace(ConfirmationCompany) &&
        !string.IsNullOrWhiteSpace(ConfirmationPhoneNumber) &&
        !string.IsNullOrWhiteSpace(ConfirmationObjective);

    public string State => attempt?.State.ToString() ?? "Unavailable";
    public string LastError => lastResult.Succeeded ? string.Empty : lastResult.Code;
    public OperationResult LastResult => lastResult;

    public ICommand ReviewPreflightCommand { get; }
    public ICommand RequestDialConfirmationCommand { get; }
    public ICommand ConfirmAndDialCommand { get; }
    public ICommand CancelDialCommand { get; }

    public Task<OperationResult> ReviewPreflightAsync(CancellationToken cancellationToken = default) =>
        ExecuteLockedAsync(ReviewPreflightCore, cancellationToken);

    public Task<OperationResult> RequestDialConfirmationAsync(CancellationToken cancellationToken = default) =>
        ExecuteLockedAsync(RequestDialConfirmationCore, cancellationToken);

    public Task<OperationResult> ConfirmAndDialAsync(CancellationToken cancellationToken = default) =>
        ExecuteConfirmAndDialAsync(cancellationToken);

    public Task<OperationResult> CancelDialAsync(CancellationToken cancellationToken = default) =>
        ExecuteLockedAsync(CancelDialCore, cancellationToken);

    public OperationResult ReviewPreflight() => ExecuteSynchronously(ReviewPreflightCore);
    public OperationResult RequestDialConfirmation() => ExecuteSynchronously(RequestDialConfirmationCore);
    public OperationResult CancelDial() => ExecuteSynchronously(CancelDialCore);

    private async Task<OperationResult> ExecuteConfirmAndDialAsync(CancellationToken cancellationToken)
    {
        await commandGate.WaitAsync(cancellationToken);
        try { return await ConfirmAndDialCoreAsync(cancellationToken); }
        finally { commandGate.Release(); }
    }

    private async Task<OperationResult> ConfirmAndDialCoreAsync(CancellationToken cancellationToken)
    {
        if (!ConfirmationOpen) return SetLastResult(OperationResult.Fail("ConfirmationNotOpen"));
        if (!simulationMode) return BlockConfirmation("SimulationOnly");
        if (!supervisionPresent) return BlockConfirmation("SupervisionRequired");
        if (stateUncertain) return BlockConfirmation("UncertainState");
        if (secondInstanceDetected) return BlockConfirmation("SecondInstance");
        if (storageFailure) return BlockConfirmation("StorageFailure");
        if (attempt is null || revision is null || authorization is null || callAttemptCoordinator is null)
            return BlockConfirmation("ControllerUnavailable");
        if (!IsCurrentRevision()) return BlockConfirmation("PreflightChanged");

        if (authorization.Status != DialAuthorizationStatus.PendingUse ||
            !authorization.IsValidFor(revision, supervisionPresent))
        {
            ConfirmationConsumed = authorization.Consumed;
            return BlockConfirmation(authorization.Consumed ? "ConsumedConfirmation" : "AuthorizationInvalid");
        }

        var authorized = attempt.MoveTo(AttemptState.Authorized);
        if (!authorized.Succeeded) return BlockConfirmation("StateBlocked");

        OperationResult result;
        try
        {
            result = await callAttemptCoordinator.ConfirmAndDialAsync(
                attempt, revision, authorization, simulationMode, supervisionPresent, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result = OperationResult.Fail("ControllerFailure");
        }

        ConfirmationOpen = false;
        ConfirmationConsumed = authorization.Consumed;
        if (!authorization.Consumed)
        {
            authorization.Invalidate("DialCommandFailed");
            ConfirmationInvalidated = true;
        }

        if (result.Code is "DialBlocked" or "StateBlocked" or "IntentPersistenceFailed")
            StateUncertain = true;

        RefreshState();
        return SetLastResult(result);
    }

    private OperationResult ReviewPreflightCore()
    {
        if (preflightCoordinator is null || attempt is null || request is null)
            return SetLastResult(OperationResult.Fail("ControllerUnavailable"));
        if (ConfirmationOpen || authorization?.Status == DialAuthorizationStatus.PendingUse)
            return SetLastResult(OperationResult.Fail("ConfirmationOpen"));
        if (attempt.State is not (AttemptState.Draft or AttemptState.PreflightReady or AttemptState.AwaitingConfirmation))
            return SetLastResult(OperationResult.Fail("StateBlocked"));

        var result = preflightCoordinator.Review(attempt.Id, request, out var reviewedRevision);
        if (!result.Succeeded || reviewedRevision is null)
            return SetLastResult(result.Succeeded ? OperationResult.Fail("PreflightIncomplete") : result);

        if (attempt.State == AttemptState.Draft && !attempt.MoveTo(AttemptState.PreflightReady).Succeeded)
            return SetLastResult(OperationResult.Fail("StateBlocked"));

        authorization?.Invalidate("PreflightReviewedAgain");
        authorization = null;
        revision = reviewedRevision;
        ConfirmationOpen = false;
        ConfirmationConsumed = false;
        ConfirmationInvalidated = false;
        StateUncertain = false;
        SetConfirmationBinding(null);
        OnPropertyChanged(nameof(Revision));
        OnPropertyChanged(nameof(Authorization));
        RefreshState();
        return SetLastResult(OperationResult.Ok("PreflightReviewed"));
    }

    private OperationResult RequestDialConfirmationCore()
    {
        if (!simulationMode) return SetLastResult(OperationResult.Fail("SimulationOnly"));
        if (!supervisionPresent) return SetLastResult(OperationResult.Fail("SupervisionRequired"));
        if (attempt is null || revision is null || request is null)
            return SetLastResult(OperationResult.Fail("PreflightIncomplete"));
        if (!IsCurrentRevision()) return BlockConfirmation("PreflightChanged");
        if (attempt.State is not (AttemptState.PreflightReady or AttemptState.AwaitingConfirmation))
            return SetLastResult(OperationResult.Fail("StateBlocked"));
        if (ConfirmationOpen && authorization?.Status == DialAuthorizationStatus.PendingUse)
            return SetLastResult(OperationResult.Ok("ConfirmationAlreadyOpen"));

        if (attempt.State == AttemptState.PreflightReady && !attempt.MoveTo(AttemptState.AwaitingConfirmation).Succeeded)
            return SetLastResult(OperationResult.Fail("StateBlocked"));

        authorization = new DialAuthorization(attempt.Id, revision, supervisionPresent);
        if (authorization.Status != DialAuthorizationStatus.PendingUse)
            return BlockConfirmation("AuthorizationInvalid");

        SetConfirmationBinding(revision);
        ConfirmationConsumed = false;
        ConfirmationInvalidated = false;
        ConfirmationOpen = true;
        OnPropertyChanged(nameof(Authorization));
        RefreshState();
        return SetLastResult(OperationResult.Ok("ConfirmationOpened"));
    }

    private OperationResult CancelDialCore()
    {
        if (!ConfirmationOpen) return SetLastResult(OperationResult.Fail("ConfirmationNotOpen"));
        if (authorization?.Consumed == true)
        {
            ConfirmationOpen = false;
            ConfirmationConsumed = true;
            return SetLastResult(OperationResult.Fail("ConsumedConfirmation"));
        }

        InvalidatePendingConfirmation("OperatorCancelled");
        return SetLastResult(OperationResult.Ok("DialCancelled"));
    }

    private async Task<OperationResult> ExecuteLockedAsync(Func<OperationResult> operation, CancellationToken cancellationToken)
    {
        await commandGate.WaitAsync(cancellationToken);
        try { return operation(); }
        finally { commandGate.Release(); }
    }

    private OperationResult ExecuteSynchronously(Func<OperationResult> operation)
    {
        commandGate.Wait();
        try { return operation(); }
        finally { commandGate.Release(); }
    }

    private OperationResult BlockConfirmation(string code)
    {
        InvalidatePendingConfirmation(code);
        return SetLastResult(OperationResult.Fail(code));
    }

    private void InvalidatePendingConfirmation(string reason)
    {
        if (authorization is not null)
        {
            authorization.Invalidate(reason);
            ConfirmationConsumed = authorization.Consumed;
        }

        if (ConfirmationOpen || authorization is not null)
            ConfirmationInvalidated = !ConfirmationConsumed;
        ConfirmationOpen = false;
        RefreshState();
    }

    private bool IsCurrentRevision() =>
        attempt is not null && revision is not null && request is not null &&
        revision.AttemptId == attempt.Id && RequestsEqual(revision.Request, request);

    private void SetConfirmationBinding(PreflightRevision? value)
    {
        ConfirmationCompany = value?.Request.Company ?? string.Empty;
        ConfirmationPhoneNumber = value?.Request.PhoneNumber ?? string.Empty;
        ConfirmationObjective = value?.Request.Objective ?? string.Empty;
    }

    private void RefreshState()
    {
        var canPrepare = request is not null && attempt is not null &&
            attempt.State is (AttemptState.Draft or AttemptState.PreflightReady or AttemptState.AwaitingConfirmation) &&
            !ConfirmationOpen && !confirmationConsumed && !stateUncertain &&
            !secondInstanceDetected && !storageFailure;
        SetStateProperty(ref ReviewEnabled, canPrepare, nameof(ReviewEnabled));

        var canRequest = simulationMode && supervisionPresent && revision is not null &&
            IsCurrentRevision() && attempt is not null &&
            attempt.State is (AttemptState.PreflightReady or AttemptState.AwaitingConfirmation) &&
            !confirmationConsumed && !stateUncertain && !secondInstanceDetected && !storageFailure;
        SetStateProperty(ref CanRequestDialConfirmation, canRequest, nameof(CanRequestDialConfirmation));

        var canConfirm = simulationMode && supervisionPresent && ConfirmationOpen &&
            authorization?.Status == DialAuthorizationStatus.PendingUse && revision is not null &&
            IsCurrentRevision() && !stateUncertain && !secondInstanceDetected && !storageFailure;
        SetStateProperty(ref CanConfirmAndDial, canConfirm, nameof(CanConfirmAndDial));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(ShowsCompanyNumberAndObjective));
        RaiseCommandCanExecuteChanged();
    }

    private OperationResult SetLastResult(OperationResult result)
    {
        lastResult = result;
        OnPropertyChanged(nameof(LastResult));
        OnPropertyChanged(nameof(LastError));
        RefreshState();
        return result;
    }

    private static bool RequestsEqual(CallRequest? left, CallRequest? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null || left.Company != right.Company ||
            left.PhoneNumber != right.PhoneNumber || left.Objective != right.Objective ||
            left.Equipment != right.Equipment || left.Problem != right.Problem ||
            left.PricePolicy != right.PricePolicy || left.Availability != right.Availability) return false;

        return left.Facts.SequenceEqual(right.Facts, StringComparer.Ordinal) &&
            left.Tests.SequenceEqual(right.Tests, StringComparer.Ordinal) &&
            left.Questions.SequenceEqual(right.Questions, StringComparer.Ordinal) &&
            left.DisclosureGrants.SequenceEqual(right.DisclosureGrants) &&
            left.SuccessCriteria.SequenceEqual(right.SuccessCriteria, StringComparer.Ordinal) &&
            left.HandoffTriggers.SequenceEqual(right.HandoffTriggers, StringComparer.Ordinal);
    }

    private void SetStateProperty(ref bool field, bool value, string propertyName)
    {
        if (field == value) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void RaiseCommandCanExecuteChanged()
    {
        ((AsyncCommand)ReviewPreflightCommand).RaiseCanExecuteChanged();
        ((AsyncCommand)RequestDialConfirmationCommand).RaiseCanExecuteChanged();
        ((AsyncCommand)ConfirmAndDialCommand).RaiseCanExecuteChanged();
        ((AsyncCommand)CancelDialCommand).RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged(string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
    {
        private bool executing;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !executing && canExecute();

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter)) _ = ExecuteAsync();
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        private async Task ExecuteAsync()
        {
            executing = true;
            RaiseCanExecuteChanged();
            try { await execute(); }
            catch (OperationCanceledException) { }
            finally
            {
                executing = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}
