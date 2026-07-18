declare module '*.module.css';

interface Window {
  MusicTubearr: {
    apiKey: string;
    instanceName: string;
    theme: string;
    urlBase: string;
    version: string;
    isProduction: boolean;
  };
}
