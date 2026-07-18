import PropTypes from 'prop-types';
import React from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { icons, inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const writeAudioTagOptions = [
  { key: 'sync', value: 'All files; keep in sync with MusicTubearr metadata' },
  { key: 'allFiles', value: 'All files; initial import only' },
  { key: 'newFiles', value: 'For new downloads only' },
  { key: 'no', value: 'Never' }
];

const youtubeAudioFormatOptions = [
  { key: 'mp3', value: 'MP3' },
  { key: 'm4a', value: 'M4A / AAC' },
  { key: 'flac', value: 'FLAC' },
  { key: 'opus', value: 'Opus' },
  { key: 'wav', value: 'WAV' }
];

function MetadataProvider(props) {
  const {
    isFetching,
    error,
    settings,
    hasSettings,
    isDownloadingYtDlp,
    isDownloadingFfmpeg,
    isUploadingCookies,
    toolsMessage,
    onInputChange,
    onDownloadYtDlpPress,
    onDownloadFfmpegPress,
    onUploadCookiesPress
  } = props;

  return (

    <div>
      {
        isFetching &&
          <LoadingIndicator />
      }

      {
        !isFetching && error &&
          <Alert kind={kinds.DANGER}>
            {translate('UnableToLoadMetadataProviderSettings')}
          </Alert>
      }

      {
        hasSettings && !isFetching && !error &&
          <Form>
            <FieldSet legend="YouTube / yt-dlp">
              {
                toolsMessage &&
                  <Alert kind={kinds.INFO}>
                    {toolsMessage}
                  </Alert>
              }

              <FormGroup>
                <FormLabel>
                  Cookies File
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.PATH}
                  name="youtubeCookiesPath"
                  helpText="Netscape cookies.txt for YouTube auth. Click Upload to choose a file."
                  buttons={[
                    <FormInputButton
                      key="upload"
                      kind={kinds.PRIMARY}
                      onPress={onUploadCookiesPress}
                    >
                      <Icon
                        name={icons.DOWNLOAD}
                        isSpinning={isUploadingCookies}
                      />
                      {' '}Upload
                    </FormInputButton>
                  ]}
                  onChange={onInputChange}
                  {...settings.youtubeCookiesPath}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  yt-dlp Path
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="ytDlpPath"
                  helpText="Leave empty / click Download and MusicTubearr will fetch yt-dlp for you"
                  buttons={[
                    <FormInputButton
                      key="download-ytdlp"
                      kind={kinds.PRIMARY}
                      onPress={onDownloadYtDlpPress}
                    >
                      <Icon
                        name={icons.DOWNLOAD}
                        isSpinning={isDownloadingYtDlp}
                      />
                      {' '}Download
                    </FormInputButton>
                  ]}
                  onChange={onInputChange}
                  {...settings.ytDlpPath}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  FFmpeg Path
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="ffmpegPath"
                  helpText="On Windows, Download fetches ffmpeg automatically. Docker images already include it."
                  buttons={[
                    <FormInputButton
                      key="download-ffmpeg"
                      kind={kinds.PRIMARY}
                      onPress={onDownloadFfmpegPress}
                    >
                      <Icon
                        name={icons.DOWNLOAD}
                        isSpinning={isDownloadingFfmpeg}
                      />
                      {' '}Download
                    </FormInputButton>
                  ]}
                  onChange={onInputChange}
                  {...settings.ffmpegPath}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Audio Format
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="youtubeAudioFormat"
                  helpText="Default format for yt-dlp/ffmpeg conversion"
                  values={youtubeAudioFormatOptions}
                  onChange={onInputChange}
                  {...settings.youtubeAudioFormat}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Audio Quality
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="youtubeAudioQuality"
                  helpText="yt-dlp --audio-quality value (0 = best)"
                  onChange={onInputChange}
                  {...settings.youtubeAudioQuality}
                />
              </FormGroup>
            </FieldSet>

            <FieldSet legend={translate('WriteMetadataToAudioFiles')}>
              <FormGroup>
                <FormLabel>
                  {translate('TagAudioFilesWithMetadata')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="writeAudioTags"
                  helpTextWarning={translate('WriteAudioTagsHelpTextWarning')}
                  helpLink="https://github.com/tjet009/Musictubearr/settings#write-metadata-to-audio-files"
                  values={writeAudioTagOptions}
                  onChange={onInputChange}
                  {...settings.writeAudioTags}
                />
              </FormGroup>

              {
                settings.writeAudioTags.value !== 'no' &&
                  <FormGroup>
                    <FormLabel>
                      {translate('EmbedCoverArtInAudioFiles')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="embedCoverArt"
                      helpText={translate('EmbedCoverArtHelpText')}
                      onChange={onInputChange}
                      {...settings.embedCoverArt}
                    />
                  </FormGroup>
              }

              <FormGroup>
                <FormLabel>
                  {translate('ScrubExistingTags')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="scrubAudioTags"
                  helpText={translate('ScrubAudioTagsHelpText')}
                  onChange={onInputChange}
                  {...settings.scrubAudioTags}
                />
              </FormGroup>

            </FieldSet>
          </Form>
      }
    </div>

  );
}

MetadataProvider.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  isDownloadingYtDlp: PropTypes.bool.isRequired,
  isDownloadingFfmpeg: PropTypes.bool.isRequired,
  isUploadingCookies: PropTypes.bool.isRequired,
  toolsMessage: PropTypes.string,
  onInputChange: PropTypes.func.isRequired,
  onDownloadYtDlpPress: PropTypes.func.isRequired,
  onDownloadFfmpegPress: PropTypes.func.isRequired,
  onUploadCookiesPress: PropTypes.func.isRequired
};

MetadataProvider.defaultProps = {
  isDownloadingYtDlp: false,
  isDownloadingFfmpeg: false,
  isUploadingCookies: false,
  toolsMessage: null
};

export default MetadataProvider;
