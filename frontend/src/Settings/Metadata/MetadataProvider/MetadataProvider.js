import PropTypes from 'prop-types';
import React from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const writeAudioTagOptions = [
  { key: 'sync', value: 'All files; keep in sync with MusicBrainz' },
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
    onInputChange
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
              <FormGroup>
                <FormLabel>
                  Cookies File
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.PATH}
                  name="youtubeCookiesPath"
                  helpText="Netscape cookies.txt for YouTube auth (export from a private browser session, like Stacher)"
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
                  helpText="Path to the yt-dlp executable (default: yt-dlp on PATH)"
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
                  helpText="Path to ffmpeg binary or directory (default: ffmpeg on PATH)"
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
                  helpLink="https://wiki.servarr.com/lidarr/settings#write-metadata-to-audio-files"
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
  onInputChange: PropTypes.func.isRequired
};

export default MetadataProvider;
