﻿using System;
using System.ComponentModel.Composition;
using System.Reactive.Linq;
using GitHub.Authentication;
using GitHub.Exports;
using GitHub.Info;
using GitHub.Services;
using GitHub.Validation;
using NullGuard;
using Octokit;
using ReactiveUI;

namespace GitHub.ViewModels
{
    [ExportViewModel(ViewType = UIViewType.TwoFactor)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TwoFactorDialogViewModel : BaseViewModel, ITwoFactorDialogViewModel
    {
        bool isAuthenticationCodeSent;
        string authenticationCode;
        TwoFactorType twoFactorType;
        readonly ObservableAsPropertyHelper<string> description;
        readonly ObservableAsPropertyHelper<bool> isSms;

        [ImportingConstructor]
        public TwoFactorDialogViewModel(IVisualStudioBrowser browser,
            ITwoFactorChallengeHandler twoFactorChallengeHandler)
        {
            Title = "Two-Factor authentication required";
            twoFactorChallengeHandler.SetViewModel(this);

            OkCommand = ReactiveCommand.Create(this.WhenAny(x => x.AuthenticationCode,
                code => !string.IsNullOrEmpty(code.Value) && code.Value.Length == 6));
            CancelCommand = ReactiveCommand.Create();
            NavigateLearnMore = ReactiveCommand.Create();
            NavigateLearnMore.Subscribe(x => browser.OpenUrl(GitHubUrls.TwoFactorLearnMore));
            ResendCodeCommand = new ReactiveCommand<RecoveryOptionResult>(Observable.Return(true), _ => null);

            description = this.WhenAny(x => x.TwoFactorType, x => x.Value)
                .Select(type =>
                {
                    switch (type)
                    {
                        case TwoFactorType.Sms:
                            return "We sent you a message via SMS with your authentication code.";
                        case TwoFactorType.AuthenticatorApp:
                            return "Open the two-factor authentication app on your device to view your " +
                                "authentication code.";
                        case TwoFactorType.Unknown:
                            return "Enter a login authentication code here";

                        default:
                            return null;
                    }
                })
                .ToProperty(this, x => x.Description);

            isShowing = this.WhenAny(x => x.TwoFactorType, x => x.Value)
                .Select(factorType => factorType != TwoFactorType.None)
                .ToProperty(this, x => x.IsShowing);

            isSms = this.WhenAny(x => x.TwoFactorType, x => x.Value)
                .Select(factorType => factorType == TwoFactorType.Sms)
                .ToProperty(this, x => x.IsSms);
        }

        public IObservable<RecoveryOptionResult> Show(UserError userError)
        {
            TwoFactorRequiredUserError error = userError as TwoFactorRequiredUserError;
            TwoFactorType = error.TwoFactorType;
            var ok = OkCommand
                .Select(_ => AuthenticationCode == null
                    ? RecoveryOptionResult.CancelOperation
                    : RecoveryOptionResult.RetryOperation)
                .Do(_ => error.ChallengeResult = AuthenticationCode != null
                    ? new TwoFactorChallengeResult(AuthenticationCode)
                    : null);
            var resend = ResendCodeCommand.Select(_ => RecoveryOptionResult.RetryOperation)
                .Do(_ => error.ChallengeResult = TwoFactorChallengeResult.RequestResendCode);
            var cancel = CancelCommand.Select(_ => RecoveryOptionResult.CancelOperation);
            return Observable.Merge(ok, cancel, resend)
                .Take(1)
                .Do(_ =>
                {
                    bool authenticationCodeSent = error.ChallengeResult == TwoFactorChallengeResult.RequestResendCode;
                    if (!authenticationCodeSent)
                    {
                        TwoFactorType = TwoFactorType.None;
                    }
                    IsAuthenticationCodeSent = authenticationCodeSent;
                });
        }

        public TwoFactorType TwoFactorType
        {
            get { return twoFactorType; }
            private set { this.RaiseAndSetIfChanged(ref twoFactorType, value); }
        }

        public bool IsSms { get { return isSms.Value; } }

        public bool IsAuthenticationCodeSent
        {
            get { return isAuthenticationCodeSent; }
            private set { this.RaiseAndSetIfChanged(ref isAuthenticationCodeSent, value); }
        }

        public string Description
        {
            [return: AllowNull]
            get { return description.Value; }
        }

        [AllowNull]
        public string AuthenticationCode
        {
            [return: AllowNull]
            get { return authenticationCode; }
            set { this.RaiseAndSetIfChanged(ref authenticationCode, value); }
        }

        public ReactiveCommand<object> OkCommand { get; private set; }
        public ReactiveCommand<object> NavigateLearnMore { get; private set; }
        public ReactiveCommand<RecoveryOptionResult> ResendCodeCommand { get; private set; }
        public ReactivePropertyValidator AuthenticationCodeValidator { get; private set; }
    }
}
