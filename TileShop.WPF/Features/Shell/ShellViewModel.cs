﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using AvalonDock;
using ImageMagitek.Services;
using Jot;
using ModernWpf;
using Stylet;
using TileShop.Shared.EventModels;

namespace TileShop.WPF.ViewModels;

public class ShellViewModel : Screen, IHandle<ShowToolWindowEvent> {
	private readonly Tracker _tracker;
	private readonly IEventAggregator _events;
	private readonly IProjectService _projectService;

	private MenuViewModel _activeMenu;
	public MenuViewModel ActiveMenu {
		get => _activeMenu;
		set => SetAndNotify(ref _activeMenu, value);
	}

	private ProjectTreeViewModel _activeTree;
	public ProjectTreeViewModel ActiveTree {
		get => _activeTree;
		set => SetAndNotify(ref _activeTree, value);
	}

	private StatusBarViewModel _activeStatusBar;
	public StatusBarViewModel ActiveStatusBar {
		get => _activeStatusBar;
		set => SetAndNotify(ref _activeStatusBar, value);
	}

	private EditorsViewModel _editors;
	public EditorsViewModel Editors {
		get => _editors;
		set => SetAndNotify(ref _editors, value);
	}

	private BindableCollection<ToolViewModel> _tools = new BindableCollection<ToolViewModel>();
	public BindableCollection<ToolViewModel> Tools {
		get => _tools;
		set => SetAndNotify(ref _tools, value);
	}

	public ApplicationTheme Theme {
		get => ThemeManager.Current.ActualApplicationTheme;
		set => Dispatcher.CurrentDispatcher.Invoke(() => ThemeManager.Current.SetValue(ThemeManager.ApplicationThemeProperty, value));
	}

	public ShellViewModel(Tracker tracker, IEventAggregator events, IProjectService projectService, MenuViewModel activeMenu,
		ProjectTreeViewModel activeTree, StatusBarViewModel activeStatusBar, EditorsViewModel editors) {
		_tracker = tracker;
		_events = events;
		_events.Subscribe(this);
		_projectService = projectService;

		Editors = editors;
		Editors.Shell = this;

		ActiveMenu = activeMenu;
		ActiveMenu.Shell = this;
		ActiveTree = activeTree;
		ActiveStatusBar = activeStatusBar;

		Tools.Add(activeTree);
	}

	public void Closing(CancelEventArgs e) {
		if (Editors.RequestSaveAllUserChanges()) {
			_projectService.CloseProjects();
			_tracker.PersistAll();
		} else {
			e.Cancel = true;
		}
	}

	public void DocumentClosing(object sender, DocumentClosingEventArgs e) {
		if (e.Document.Content is ResourceEditorBaseViewModel editor) {
			if (Editors.RequestSaveUserChanges(editor, true)) {
				_ = Editors.Editors.Remove(editor);
				Editors.ActiveEditor = Editors.Editors.FirstOrDefault();
			} else {
				e.Cancel = true;
			}
		}
	}

	public void DocumentClosed(DocumentClosedEventArgs e) { }

	public void Handle(ShowToolWindowEvent message) {
		switch (message.ToolWindow) {
			case ToolWindow.ProjectExplorer:
				if (!ActiveTree.IsVisible) {
					ActiveTree.IsVisible = true;
					ActiveTree.IsSelected = true;
					_ = Tools.Remove(ActiveTree);
					Tools.Add(ActiveTree);
				}

				break;

			default:
				throw new InvalidOperationException();
		}
	}

	public void RequestApplicationExit() {
		if (Editors.RequestSaveAllUserChanges()) {
			_projectService.CloseProjects();
			_tracker.PersistAll();
			Environment.Exit(0);
		}
	}
}
