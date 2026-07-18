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
import Link from 'Components/Link/Link';
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
  { key: 'aac', value: 'AAC (iTunes)' },
  { key: 'alac', value: 'ALAC (iTunes Lossless)' },
  { key: 'm4a', value: 'M4A / AAC' },
  { key: 'flac', value: 'FLAC' },
  { key: 'opus', value: 'Opus' },
  { key: 'wav', value: 'WAV' }
];

const sponsorBlockModeOptions = [
  { key: 'off', value: 'Off' },
  { key: 'music', value: 'Music videos (recommended)' },
  { key: 'aggressive', value: 'Aggressive (more cuts)' },
  { key: 'custom', value: 'Custom categories' }
];

function cookiesAlertKind(status) {
  if (!status) {
    return kinds.INFO;
  }

  if (status.fileExists && status.hasLoginCookies) {
    return kinds.SUCCESS;
  }

  if (status.fileExists) {
    return kinds.WARNING;
  }

  return kinds.INFO;
}

function MetadataProvider(props) {
  const {
    isFetching,
    error,
    settings,
    hasSettings,
    isDownloadingYtDlp,
    isDownloadingFfmpeg,
    isUploadingCookies,
    isSavingCookiesPaste,
    isTestingCookies,
    isClearingCookies,
    isImportingCookies,
    cookiesPaste,
    cookiesStatus,
    toolsMessage,
    onInputChange,
    onDownloadYtDlpPress,
    onDownloadFfmpegPress,
    onUploadCookiesPress,
    onCookiesPasteChange,
    onSaveCookiesPastePress,
    onTestCookiesPress,
    onClearCookiesPress,
    onImportCookiesPress
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

              <Alert kind={cookiesAlertKind(cookiesStatus)}>
                {
                  cookiesStatus?.message ||
                    'YouTube usually requires cookies. Use one of the options below.'
                }
              </Alert>

              <Alert kind={kinds.INFO}>
                <div>
                  <strong>How to get cookies (pick one)</strong>
                </div>
                <ol style={{ margin: '8px 0 0', paddingLeft: '20px' }}>
                  <li>
                    Open a private/incognito window and sign in to YouTube.
                  </li>
                  <li>
                    Export with an extension:
                    {' '}
                    <Link
                      to="https://chromewebstore.google.com/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc"
                      target="_blank"
                    >
                      Get cookies.txt LOCALLY (Chrome)
                    </Link>
                    {', '}
                    <Link
                      to="https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/"
                      target="_blank"
                    >
                      cookies.txt (Firefox)
                    </Link>
                    {', or '}
                    <Link
                      to="https://chromewebstore.google.com/detail/cookie-editor/hlkenndednhfkekhgcdicdfddnkalmdm"
                      target="_blank"
                    >
                      Cookie-Editor
                    </Link>
                    {' '}
                    (export Netscape or copy the Cookie header).
                  </li>
                  <li>
                    Upload the file, paste below, or for Docker drop the file at
                    {' '}
                    <code>docker/cookies/cookies.txt</code>
                    {' '}
                    and click Import.
                  </li>
                  <li>
                    Close the private window. Re-export when downloads start failing with “Sign in” / bot checks.
                  </li>
                </ol>
              </Alert>

              <FormGroup>
                <FormLabel>
                  Cookies File
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.PATH}
                  name="youtubeCookiesPath"
                  helpText="Saved Netscape cookies.txt path. Prefer Upload / Paste / Import instead of typing a path."
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
                    </FormInputButton>,
                    <FormInputButton
                      key="import"
                      kind={kinds.DEFAULT}
                      onPress={onImportCookiesPress}
                    >
                      <Icon
                        name={icons.REFRESH}
                        isSpinning={isImportingCookies}
                      />
                      {' '}Import
                    </FormInputButton>,
                    <FormInputButton
                      key="test"
                      kind={kinds.DEFAULT}
                      onPress={onTestCookiesPress}
                    >
                      <Icon
                        name={icons.CHECK}
                        isSpinning={isTestingCookies}
                      />
                      {' '}Test
                    </FormInputButton>,
                    <FormInputButton
                      key="clear"
                      kind={kinds.DANGER}
                      onPress={onClearCookiesPress}
                    >
                      <Icon
                        name={icons.REMOVE}
                        isSpinning={isClearingCookies}
                      />
                      {' '}Clear
                    </FormInputButton>
                  ]}
                  onChange={onInputChange}
                  {...settings.youtubeCookiesPath}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Paste Cookies
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT_AREA}
                  name="cookiesPaste"
                  helpText="Paste Netscape cookies.txt contents, or a Cookie header (name=value; name2=value2). MusicTubearr converts headers automatically."
                  buttons={[
                    <FormInputButton
                      key="save-paste"
                      kind={kinds.PRIMARY}
                      onPress={onSaveCookiesPastePress}
                    >
                      <Icon
                        name={icons.SAVE}
                        isSpinning={isSavingCookiesPaste}
                      />
                      {' '}Save Paste
                    </FormInputButton>
                  ]}
                  onChange={onCookiesPasteChange}
                  value={cookiesPaste}
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
                  helpText="Default format for yt-dlp/ffmpeg conversion. Use AAC or ALAC for iTunes/Apple Music."
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

              <FormGroup>
                <FormLabel>
                  SponsorBlock
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="youtubeSponsorBlockMode"
                  helpText="Uses community SponsorBlock data via yt-dlp to cut intros, outros, sponsors, and non-music sections. Only works when segments exist for that video."
                  values={sponsorBlockModeOptions}
                  onChange={onInputChange}
                  {...settings.youtubeSponsorBlockMode}
                />
              </FormGroup>

              {
                settings.youtubeSponsorBlockMode?.value === 'custom' &&
                  <FormGroup>
                    <FormLabel>
                      SponsorBlock Categories
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.TEXT}
                      name="youtubeSponsorBlockCategories"
                      helpText="Comma-separated yt-dlp categories, e.g. intro,outro,sponsor,selfpromo,music_offtopic"
                      onChange={onInputChange}
                      {...settings.youtubeSponsorBlockCategories}
                    />
                  </FormGroup>
              }

              <FormGroup>
                <FormLabel>
                  Exclude Shorts
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="youtubeExcludeShorts"
                  helpText="Skip YouTube Shorts (≈60s or less, /shorts/ URLs, #shorts titles)"
                  onChange={onInputChange}
                  {...settings.youtubeExcludeShorts}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Music / Music Videos Only
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="youtubeMusicOnly"
                  helpText="Prefer official audio / music videos; filter reactions, podcasts, gameplay, and similar non-music clutter"
                  onChange={onInputChange}
                  {...settings.youtubeMusicOnly}
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
  isSavingCookiesPaste: PropTypes.bool.isRequired,
  isTestingCookies: PropTypes.bool.isRequired,
  isClearingCookies: PropTypes.bool.isRequired,
  isImportingCookies: PropTypes.bool.isRequired,
  cookiesPaste: PropTypes.string.isRequired,
  cookiesStatus: PropTypes.object,
  toolsMessage: PropTypes.string,
  onInputChange: PropTypes.func.isRequired,
  onDownloadYtDlpPress: PropTypes.func.isRequired,
  onDownloadFfmpegPress: PropTypes.func.isRequired,
  onUploadCookiesPress: PropTypes.func.isRequired,
  onCookiesPasteChange: PropTypes.func.isRequired,
  onSaveCookiesPastePress: PropTypes.func.isRequired,
  onTestCookiesPress: PropTypes.func.isRequired,
  onClearCookiesPress: PropTypes.func.isRequired,
  onImportCookiesPress: PropTypes.func.isRequired
};

MetadataProvider.defaultProps = {
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

export default MetadataProvider;
