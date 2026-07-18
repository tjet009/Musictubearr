import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { fetchMetadataProvider, saveMetadataProvider, setMetadataProviderValue } from 'Store/Actions/settingsActions';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import MetadataProvider from './MetadataProvider';

const SECTION = 'metadataProvider';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    createSettingsSectionSelector(SECTION),
    (advancedSettings, sectionSettings) => {
      return {
        advancedSettings,
        ...sectionSettings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchFetchMetadataProvider: fetchMetadataProvider,
  dispatchSetMetadataProviderValue: setMetadataProviderValue,
  dispatchSaveMetadataProvider: saveMetadataProvider,
  dispatchClearPendingChanges: clearPendingChanges
};

class MetadataProviderConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDownloadingYtDlp: false,
      isDownloadingFfmpeg: false,
      isUploadingCookies: false,
      isSavingCookiesPaste: false,
      isTestingCookies: false,
      isClearingCookies: false,
      isImportingCookies: false,
      cookiesPaste: '',
      cookiesStatus: null,
      toolsMessage: null
    };

    this._fileInput = null;
  }

  componentDidMount() {
    const {
      dispatchFetchMetadataProvider,
      dispatchSaveMetadataProvider,
      onChildMounted
    } = this.props;

    dispatchFetchMetadataProvider();
    onChildMounted(dispatchSaveMetadataProvider);

    this._fileInput = document.createElement('input');
    this._fileInput.type = 'file';
    this._fileInput.accept = '.txt,text/plain';
    this._fileInput.style.display = 'none';
    this._fileInput.addEventListener('change', this.onCookiesFileSelected);
    document.body.appendChild(this._fileInput);

    this.fetchCookiesStatus();
  }

  componentDidUpdate(prevProps) {
    const {
      hasPendingChanges,
      isSaving,
      onChildStateChange
    } = this.props;

    if (
      prevProps.isSaving !== isSaving ||
      prevProps.hasPendingChanges !== hasPendingChanges
    ) {
      onChildStateChange({
        isSaving,
        hasPendingChanges
      });
    }
  }

  componentWillUnmount() {
    this.props.dispatchClearPendingChanges({ section: 'settings.metadataProvider' });

    if (this._fileInput) {
      this._fileInput.removeEventListener('change', this.onCookiesFileSelected);
      document.body.removeChild(this._fileInput);
      this._fileInput = null;
    }
  }

  //
  // Control

  applyCookiesResult = (data, fallbackMessage) => {
    const path = data.path || data.youtubeCookiesPath;
    if (path || data.youtubeCookiesPath === '') {
      this.props.dispatchSetMetadataProviderValue({
        name: 'youtubeCookiesPath',
        value: path || ''
      });
      this.props.dispatchSaveMetadataProvider();
    }

    this.setState({
      toolsMessage: data.message || fallbackMessage,
      cookiesStatus: data.status || this.state.cookiesStatus,
      cookiesPaste: data.isValid ? '' : this.state.cookiesPaste
    });
  };

  fetchCookiesStatus = () => {
    const { request } = createAjaxRequest({
      url: '/config/youtube/cookies/status',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({ cookiesStatus: data });
    });
  };

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetMetadataProviderValue({ name, value });
  };

  onCookiesPasteChange = ({ value }) => {
    this.setState({ cookiesPaste: value });
  };

  onDownloadYtDlpPress = () => {
    this.setState({ isDownloadingYtDlp: true, toolsMessage: 'Downloading yt-dlp...' });

    const { request, abortRequest } = createAjaxRequest({
      url: '/config/youtube/download-ytdlp?force=true',
      method: 'POST',
      dataType: 'json'
    });

    this._abortYtDlp = abortRequest;

    request.done((data) => {
      if (data.path) {
        this.props.dispatchSetMetadataProviderValue({ name: 'ytDlpPath', value: data.path });
        this.props.dispatchSaveMetadataProvider();
      }

      this.setState({
        isDownloadingYtDlp: false,
        toolsMessage: data.message || (data.isValid ? 'yt-dlp ready' : 'yt-dlp download failed')
      });
    });

    request.fail((xhr) => {
      this.setState({
        isDownloadingYtDlp: false,
        toolsMessage: `yt-dlp download failed: ${xhr.responseText || xhr.statusText}`
      });
    });
  };

  onDownloadFfmpegPress = () => {
    this.setState({ isDownloadingFfmpeg: true, toolsMessage: 'Downloading ffmpeg...' });

    const { request, abortRequest } = createAjaxRequest({
      url: '/config/youtube/download-ffmpeg?force=true',
      method: 'POST',
      dataType: 'json'
    });

    this._abortFfmpeg = abortRequest;

    request.done((data) => {
      if (data.path) {
        this.props.dispatchSetMetadataProviderValue({ name: 'ffmpegPath', value: data.path });
        this.props.dispatchSaveMetadataProvider();
      }

      this.setState({
        isDownloadingFfmpeg: false,
        toolsMessage: data.message || (data.isValid ? 'ffmpeg ready' : 'ffmpeg download failed')
      });
    });

    request.fail((xhr) => {
      this.setState({
        isDownloadingFfmpeg: false,
        toolsMessage: `ffmpeg download failed: ${xhr.responseText || xhr.statusText}`
      });
    });
  };

  onUploadCookiesPress = () => {
    if (this._fileInput) {
      this._fileInput.value = '';
      this._fileInput.click();
    }
  };

  onCookiesFileSelected = () => {
    const file = this._fileInput && this._fileInput.files && this._fileInput.files[0];
    if (!file) {
      return;
    }

    this.setState({ isUploadingCookies: true, toolsMessage: 'Uploading cookies...' });

    const formData = new FormData();
    formData.append('file', file);

    const { request } = createAjaxRequest({
      url: '/config/youtube/cookies',
      method: 'POST',
      processData: false,
      contentType: false,
      data: formData,
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({ isUploadingCookies: false });
      this.applyCookiesResult(data, 'Cookies uploaded');
    });

    request.fail((xhr) => {
      let message = xhr.responseText || xhr.statusText;
      try {
        const parsed = JSON.parse(xhr.responseText);
        message = parsed.message || message;
      } catch (e) {
        // keep raw
      }

      this.setState({
        isUploadingCookies: false,
        toolsMessage: `Cookie upload failed: ${message}`
      });
    });
  };

  onSaveCookiesPastePress = () => {
    const content = (this.state.cookiesPaste || '').trim();
    if (!content) {
      this.setState({ toolsMessage: 'Paste Netscape cookies.txt or a Cookie header first.' });
      return;
    }

    this.setState({ isSavingCookiesPaste: true, toolsMessage: 'Saving pasted cookies...' });

    const { request } = createAjaxRequest({
      url: '/config/youtube/cookies/paste',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify({ content }),
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({ isSavingCookiesPaste: false });
      this.applyCookiesResult(data, 'Cookies saved from paste');
    });

    request.fail((xhr) => {
      let message = xhr.responseText || xhr.statusText;
      try {
        const parsed = JSON.parse(xhr.responseText);
        message = parsed.message || message;
      } catch (e) {
        // keep raw
      }

      this.setState({
        isSavingCookiesPaste: false,
        toolsMessage: `Cookie paste failed: ${message}`
      });
    });
  };

  onImportCookiesPress = () => {
    this.setState({ isImportingCookies: true, toolsMessage: 'Looking for cookies in /cookies ...' });

    const { request } = createAjaxRequest({
      url: '/config/youtube/cookies/import',
      method: 'POST',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({ isImportingCookies: false });
      this.applyCookiesResult(data, 'Cookies imported');
    });

    request.fail((xhr) => {
      let message = xhr.responseText || xhr.statusText;
      try {
        const parsed = JSON.parse(xhr.responseText);
        message = parsed.message || message;
      } catch (e) {
        // keep raw
      }

      this.setState({
        isImportingCookies: false,
        toolsMessage: `Cookie import failed: ${message}`
      });
      this.fetchCookiesStatus();
    });
  };

  onTestCookiesPress = () => {
    this.setState({ isTestingCookies: true, toolsMessage: 'Testing YouTube cookies with yt-dlp...' });

    const { request } = createAjaxRequest({
      url: '/config/youtube/test',
      method: 'POST',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({
        isTestingCookies: false,
        toolsMessage: data.message || (data.isValid ? 'Cookies OK' : 'Cookie test failed'),
        cookiesStatus: data.cookies || this.state.cookiesStatus
      });
    });

    request.fail((xhr) => {
      this.setState({
        isTestingCookies: false,
        toolsMessage: `Cookie test failed: ${xhr.responseText || xhr.statusText}`
      });
    });
  };

  onClearCookiesPress = () => {
    this.setState({ isClearingCookies: true, toolsMessage: 'Clearing cookies...' });

    const { request } = createAjaxRequest({
      url: '/config/youtube/cookies',
      method: 'DELETE',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({ isClearingCookies: false });
      this.applyCookiesResult({ ...data, isValid: true }, 'Cookies cleared');
    });

    request.fail((xhr) => {
      this.setState({
        isClearingCookies: false,
        toolsMessage: `Clear cookies failed: ${xhr.responseText || xhr.statusText}`
      });
    });
  };

  //
  // Render

  render() {
    return (
      <MetadataProvider
        {...this.props}
        {...this.state}
        onInputChange={this.onInputChange}
        onDownloadYtDlpPress={this.onDownloadYtDlpPress}
        onDownloadFfmpegPress={this.onDownloadFfmpegPress}
        onUploadCookiesPress={this.onUploadCookiesPress}
        onCookiesPasteChange={this.onCookiesPasteChange}
        onSaveCookiesPastePress={this.onSaveCookiesPastePress}
        onTestCookiesPress={this.onTestCookiesPress}
        onClearCookiesPress={this.onClearCookiesPress}
        onImportCookiesPress={this.onImportCookiesPress}
      />
    );
  }
}

MetadataProviderConnector.propTypes = {
  isSaving: PropTypes.bool.isRequired,
  hasPendingChanges: PropTypes.bool.isRequired,
  dispatchFetchMetadataProvider: PropTypes.func.isRequired,
  dispatchSetMetadataProviderValue: PropTypes.func.isRequired,
  dispatchSaveMetadataProvider: PropTypes.func.isRequired,
  dispatchClearPendingChanges: PropTypes.func.isRequired,
  onChildMounted: PropTypes.func.isRequired,
  onChildStateChange: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(MetadataProviderConnector);
