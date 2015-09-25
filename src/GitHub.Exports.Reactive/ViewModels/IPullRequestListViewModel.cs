﻿using GitHub.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GitHub.ViewModels
{
    public interface IPullRequestListViewModel
    {
        ObservableCollection<IPullRequestModel> PullRequests { get; }
        IPullRequestModel SelectedPullRequest { get; }
    }
}
