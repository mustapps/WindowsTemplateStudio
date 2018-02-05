﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Diagnostics;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Mvvm;
using Microsoft.Templates.UI.Threading;
using Microsoft.Templates.UI.V2Controls;
using Microsoft.Templates.UI.V2Resources;
using Microsoft.Templates.UI.V2Services;
using Microsoft.Templates.UI.V2ViewModels.Common;
using Microsoft.Templates.UI.V2Views.Common;
using Microsoft.Templates.UI.V2Views.NewProject;

namespace Microsoft.Templates.UI.V2ViewModels.NewProject
{
    public class MainViewModel : BaseMainViewModel
    {
        private RelayCommand _refreshTemplatesCacheCommand;

        private TemplateInfoViewModel _selectedTemplate;

        public static MainViewModel Instance { get; private set; }

        public ProjectTypeViewModel ProjectType { get; } = new ProjectTypeViewModel(() => Instance.IsSelectionEnabled(MetadataType.ProjectType));

        public FrameworkViewModel Framework { get; } = new FrameworkViewModel(() => Instance.IsSelectionEnabled(MetadataType.Framework));

        public AddPagesViewModel AddPages { get; } = new AddPagesViewModel();

        public AddFeaturesViewModel AddFeatures { get; } = new AddFeaturesViewModel();

        public UserSelectionViewModel UserSelection { get; } = new UserSelectionViewModel();

        public RelayCommand RefreshTemplatesCacheCommand => _refreshTemplatesCacheCommand ?? (_refreshTemplatesCacheCommand = new RelayCommand(
             () => SafeThreading.JoinableTaskFactory.RunAsync(async () => await OnRefreshTemplatesAsync())));

        public Visibility RefreshTemplateCacheVisibility
        {
            get
            {
#if DEBUG
                return Visibility.Visible;
#else
                return Visibility.Hidden;
#endif
            }
        }

        public MainViewModel(Window mainView)
            : base(mainView)
        {
            Instance = this;
            ValidationService.Initialize(UserSelection.GetNames);
        }

        public override async Task InitializeAsync(string language)
        {
            await base.InitializeAsync(language);
        }

        protected override void OnCancel() => WizardShell.Current.Close();

        protected override void UpdateStep()
        {
            base.UpdateStep();
            Page destinationPage = null;
            switch (Step)
            {
                case 0:
                    destinationPage = new ProjectTypePage();
                    break;
                case 1:
                    destinationPage = new FrameworkPage();
                    break;
                case 2:
                    destinationPage = new AddPagesPage();
                    break;
                case 3:
                    destinationPage = new AddFeaturesPage();
                    break;
            }

            if (destinationPage != null)
            {
                NavigationService.NavigateSecondaryFrame(destinationPage);
                SetCanGoBack(Step > 0);
                SetCanGoForward(Step < 3);
            }
        }

        protected override void OnFinish()
        {
            WizardShell.Current.Result = UserSelection.GetUserSelection();
            base.OnFinish();
        }

        public override bool IsSelectionEnabled(MetadataType metadataType)
        {
            bool result = false;
            if (!UserSelection.HasItemsAddedByUser)
            {
                result = true;
            }
            else
            {
                var vm = new QuestionDialogViewModel(metadataType);
                var questionDialog = new QuestionDialogWindow(vm);
                questionDialog.ShowDialog();

                if (vm.Result == DialogResult.Accept)
                {
                    UserSelection.ResetUserSelection();
                    result = true;
                }
                else
                {
                    result = false;
                }
            }

            if (result == true)
            {
                AddPages.ResetUserSelection();
                AddFeatures.ResetTemplatesCount();
            }

            return result;
        }

        public TemplateInfoViewModel GetTemplate(ITemplateInfo templateInfo)
        {
            var groups = templateInfo.GetTemplateType() == TemplateType.Page ? AddPages.Groups : AddFeatures.Groups;
            foreach (var group in groups)
            {
                var template = group.GetTemplate(templateInfo);
                if (template != null)
                {
                    return template;
                }
            }

            return null;
        }

        private void AddTemplate(TemplateInfoViewModel selectedTemplate)
        {
            if (selectedTemplate.MultipleInstance || !UserSelection.IsTemplateAdded(selectedTemplate))
            {
                UserSelection.Add(TemplateOrigin.UserSelection, selectedTemplate);
            }
        }

        protected override Task OnTemplatesAvailableAsync()
        {
            ProjectType.LoadData();
            return Task.CompletedTask;
        }

        protected override IEnumerable<Step> GetSteps()
        {
            yield return new Step(0, StringRes.NewProjectStepOne, true, true);
            yield return new Step(1, StringRes.NewProjectStepTwo);
            yield return new Step(2, StringRes.NewProjectStepThree);
            yield return new Step(3, StringRes.NewProjectStepFour);
        }

        public override void ProcessItem(object item)
        {
            if (item is MetadataInfoViewModel metadata)
            {
                if (metadata.MetadataType == MetadataType.ProjectType)
                {
                    if (ProjectType.Select(metadata))
                    {
                        Framework.LoadData(metadata.Name);
                        EventService.Instance.RaiseOnProjectTypeChange(metadata);
                    }
                }
                else if (metadata.MetadataType == MetadataType.Framework)
                {
                    if (Framework.Select(metadata))
                    {
                        AddPages.LoadData(metadata.Name);
                        AddFeatures.LoadData(metadata.Name);
                        UserSelection.Initialize(ProjectType.Selected.Name, Framework.Selected.Name, Language);
                        WizardStatus.IsLoading = false;
                        EventService.Instance.RaiseOnFrameworkChange(metadata);
                    }
                }
            }
            else if (item is TemplateInfoViewModel template)
            {
                _selectedTemplate = template;
                AddTemplate(template);
            }
        }

        protected async Task OnRefreshTemplatesAsync()
        {
            try
            {
                WizardStatus.IsLoading = true;
                UserSelection.ResetUserSelection();
                await GenContext.ToolBox.Repo.RefreshAsync(true);
            }
            catch (Exception ex)
            {
                await NotificationsControl.Instance.AddNotificationAsync(Notification.Error(StringRes.NotificationSyncError_Refresh));

                await AppHealth.Current.Error.TrackAsync(ex.ToString());
                await AppHealth.Current.Exception.TrackAsync(ex);
            }
            finally
            {
                WizardStatus.IsLoading = GenContext.ToolBox.Repo.SyncInProgress;
            }
        }
    }
}
