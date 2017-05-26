﻿namespace DfBAdminToolkit.Presenter {

    using Common.Services;
    using Common.Utils;
    using CsvHelper;
    using CsvHelper.Configuration;
    using Model;
    using View;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;

    public class TeamAuditingPresenter
        : PresenterBase, ITeamAuditingPresenter {

        public TeamAuditingPresenter(ITeamAuditingModel model, ITeamAuditingView view)
            : base(model, view) {
        }

        protected override void Initialize() {
            ITeamAuditingView view = base._view as ITeamAuditingView;
            ITeamAuditingModel model = base._model as ITeamAuditingModel;
            PresenterBase.SetViewPropertiesFromModel<ITeamAuditingView, ITeamAuditingModel>(
                ref view, model
            );

            SyncContext.Post(delegate {
                view.RefreshAccessToken();
            }, null);
        }

        protected override void WireViewEvents() {
            if (!IsViewEventsWired) {
                ITeamAuditingView view = base._view as ITeamAuditingView;
                view.DataChanged += DataChanged;
                //view.CommandLoadTeamFolders += OnCommandLoadTeamFolders;
                IsViewEventsWired = true;
            }
        }

        protected override void UnWireViewEvents() {
            if (IsViewEventsWired) {
                ITeamAuditingView view = base._view as ITeamAuditingView;
                view.DataChanged -= DataChanged;
                //view.CommandLoadTeamHealth -= OnCommandLoadTeamFolders;
                IsViewEventsWired = false;
            }
        }

        protected override void CleanUp() {
        }

        public void UpdateSettings() {
            DataChanged(this, new EventArgs());
        }

        #region REST Service

        private void GetPaperDocs(ITeamAuditingModel model, IMainPresenter presenter)
        {
            IMemberServices service = service = new MemberServices(ApplicationResource.BaseUrl, ApplicationResource.ApiVersion);
            service.ListTeamFolderUrl = ApplicationResource.ActionListTeamFolder;
            service.UserAgentVersion = ApplicationResource.UserAgent;
            string fileAccessToken = ApplicationResource.DefaultAccessToken;
            IDataResponse response = service.ListTeamFolders(fileAccessToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Data != null)
                {
                    string data = response.Data.ToString();
                    dynamic jsonData = JsonConvert.DeserializeObject<dynamic>(data);

                    // clear existing data first
                    //model.TeamAuditing.Clear();
                    //changed from entries to team_folders
                    int resultCount = jsonData["team_folders"].Count;
                    for (int i = 0; i < resultCount; i++)
                    {
                        dynamic team_folders = jsonData["team_folders"][i];
                        dynamic teamFolderName = team_folders["name"];
                        dynamic teamFolderId = team_folders["team_folder_id"];
                        dynamic status = team_folders["status"][".tag"];

                    // update model
                    TeamFoldersListViewItemModel lvItem = new TeamFoldersListViewItemModel()
                    {
                        TeamFolderName = teamFolderName,
                        TeamFolderId = teamFolderId,
                        Status = status,
                        IsChecked = true
                    };
                        //model.TeamAuditing.Add(lvItem);
                    }
                }
            }
        }

        #endregion REST Service

        #region Events

        private void OnCommandLoadTeamFolders(object sender, EventArgs e)
        {
            ITeamAuditingView view = base._view as ITeamAuditingView;
            ITeamAuditingModel model = base._model as ITeamAuditingModel;
            IMainPresenter presenter = SimpleResolver.Instance.Get<IMainPresenter>();
            if (SyncContext != null)
            {
                SyncContext.Post(delegate {
                    presenter.EnableControl(false);
                    presenter.ActivateSpinner(true);
                    presenter.UpdateProgressInfo("Loading team folders input File...");
                }, null);
            }
            Thread teamfoldersLoad = new Thread(() => {
                if (!string.IsNullOrEmpty(model.AccessToken))
                {
                    //bool loaded = this.LoadTeamFoldersInputFile(model, presenter);
                    if (SyncContext != null)
                    {
                        SyncContext.Post(delegate {
                            // update result and update view.
                            view.RenderTeamAuditingList();
                            presenter.UpdateProgressInfo("Team Folders CSV Loaded");
                            presenter.ActivateSpinner(false);
                            presenter.EnableControl(true);
                        }, null);
                    }
                }
            });
            teamfoldersLoad.Start();
        }

        private void DataChanged(object sender, System.EventArgs e) {
            ITeamAuditingView view = base._view as ITeamAuditingView;
            ITeamAuditingModel model = base._model as ITeamAuditingModel;
            PresenterBase.SetModelPropertiesFromView<ITeamAuditingModel, ITeamAuditingView>(
                ref model, view
            );
        }

        #endregion Events
    }
}