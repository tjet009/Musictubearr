import React from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import LoadingMessage from 'Components/Loading/LoadingMessage';
import styles from './LoadingPage.css';

function LoadingPage() {
  const logoSrc = `${window.MusicTubearr?.urlBase || ''}/Content/Images/logo.png`;

  return (
    <div className={styles.page}>
      <img
        className={styles.logoFull}
        src={logoSrc}
        alt="MusicTubearr"
      />
      <LoadingMessage />
      <LoadingIndicator />
    </div>
  );
}

export default LoadingPage;
