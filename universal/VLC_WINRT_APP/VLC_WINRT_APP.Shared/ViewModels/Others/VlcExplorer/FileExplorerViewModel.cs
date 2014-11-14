﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Search;
using Windows.UI.Core;
using VLC_WINRT_APP.Commands.RemovableDevices;
using VLC_WINRT_APP.Common;
using VLC_WINRT_APP.Model;
using VLC_WINRT_APP.Views.VideoPages;

namespace VLC_WINRT_APP.ViewModels.Others.VlcExplorer
{
    public class FileExplorerViewModel : BindableBase
    {
        #region private props
        private IStorageItemClickedCommand _navigateToCommand;
        private GoUpperFolderCommand _goUpperFolderCommand;
        private bool _isFolderEmpty;
        #endregion

        #region private fields
        private ObservableCollection<StorageFolder> _backStack = new ObservableCollection<StorageFolder>();
        private ObservableCollection<IStorageItem> _storageItems = new ObservableCollection<IStorageItem>();
        #endregion

        #region public props
        public string Id;
        public string Name { get; set; }

        public IStorageItemClickedCommand NavigateToCommand
        {
            get { return _navigateToCommand; }
            set { SetProperty(ref _navigateToCommand, value); }
        }

        public GoUpperFolderCommand GoBackCommand
        {
            get { return _goUpperFolderCommand; }
            set { SetProperty(ref _goUpperFolderCommand, value); }
        }

        public bool CanGoBack
        {
            get { return BackStack.Count > 1; }
        }

        public bool IsFolderEmpty
        {
            get { return _isFolderEmpty; }
            set { SetProperty(ref _isFolderEmpty, value); }
        }
        #endregion

        #region public fields
        public ObservableCollection<IStorageItem> StorageItems
        {
            get { return _storageItems; }
            set { SetProperty(ref _storageItems, value); }
        }

        public ObservableCollection<StorageFolder> BackStack
        {
            get { return _backStack; }
            set { SetProperty(ref _backStack, value); }
        }

        #endregion

        public FileExplorerViewModel(StorageFolder root, string id = null)
        {
            NavigateToCommand = new IStorageItemClickedCommand();
            GoBackCommand = new GoUpperFolderCommand();
            BackStack.Add(root);
            Name = root.DisplayName;

            if (id != null)
                Id = id;
        }

        public async Task GetFiles()
        {
            try
            {
                await App.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    _storageItems.Clear();
                    IsFolderEmpty = false;
                });
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, VLCFileExtensions.Supported);

                var fileQuery = BackStack.Last().CreateItemQueryWithOptions(queryOptions);
                var items = await fileQuery.GetItemsAsync();
                foreach (IStorageItem storageItem in items)
                {
                    await App.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        StorageItems.Add(storageItem);
                        OnPropertyChanged("StorageItems");
                    });
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Failed to index folders and files in " + BackStack.Last().DisplayName + "\n" + exception.ToString());
            }
            await App.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                OnPropertyChanged("CanGoBack");
                IsFolderEmpty = !StorageItems.Any();
            });
        }

        public async Task NavigateTo(IStorageItem storageItem)
        {
            if (storageItem is StorageFolder)
            {
                BackStack.Add(storageItem as StorageFolder);
                Task.Run(() => GetFiles());
            }
            else
            {
                StorageFile file = storageItem as StorageFile;
                Model.Video.VideoItem videoVm = new Model.Video.VideoItem();
                await videoVm.Initialize(file);
                if (string.IsNullOrEmpty(videoVm.Token))
                {
                    string token = StorageApplicationPermissions.FutureAccessList.Add(videoVm.File);
                    videoVm.Token = token;
                }
                Locator.VideoVm.CurrentVideo = videoVm;
                await Locator.VideoVm.SetActiveVideoInfo(videoVm.Token);
                App.ApplicationFrame.Navigate(typeof(VideoPlayerPage));
            }
        }

        public void GoBack()
        {
            if (BackStack.Count > 1)
            {
                BackStack.Remove(BackStack.Last());
                Task.Run(() => GetFiles());
                OnPropertyChanged("CanGoBack");
            }
        }
    }
}
