import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';
import styles from '../styles.css';

class Donations extends Component {

  //
  // Render

  render() {
    return (
      <FieldSet legend={translate('Donations')}>
        <div className={styles.logoContainer} title="MusicTubearr">
          <Link to="https://github.com/tjet009/Musictubearr">
            <img
              className={styles.logo}
              src={`${window.MusicTubearr.urlBase}/Content/Images/Icons/logo-musictubearr.png`}
              alt="MusicTubearr"
            />
          </Link>
        </div>
        <div className={styles.summary}>
          MusicTubearr is a YouTube-first fork. Support the project on GitHub.
        </div>
      </FieldSet>
    );
  }
}

Donations.propTypes = {
};

export default Donations;
