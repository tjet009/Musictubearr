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
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetMetadataProviderValue({ name, value });
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
      const path = data.path || data.youtubeCookiesPath;
      if (path) {
        this.props.dispatchSetMetadataProviderValue({ name: 'youtubeCookiesPath', value: path });
        this.props.dispatchSaveMetadataProvider();
      }

      this.setState({
        isUploadingCookies: false,
        toolsMessage: path ? `Cookies saved to ${path}` : 'Cookies uploaded'
      });
    });

    request.fail((xhr) => {
      this.setState({
        isUploadingCookies: false,
        toolsMessage: `Cookie upload failed: ${xhr.responseText || xhr.statusText}`
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
