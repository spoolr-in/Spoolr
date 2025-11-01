import axios from 'axios';

const API_BASE_URL = 'http://localhost:8080/api';

// User-related API calls
const USER_API_URL = `${API_BASE_URL}/users`;

export const getProfile = (token) => {
  return axios.get(`${USER_API_URL}/profile`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
};

export const verifyEmail = (token) => {
  return axios.get(`${USER_API_URL}/verify?token=${token}`);
};

export const resetPassword = (token, newPassword) => {
  return axios.post(`${USER_API_URL}/reset-password`, { token, newPassword });
};

export const requestPasswordReset = (email) => {
  return axios.post(`${USER_API_URL}/request-password-reset`, { email });
};

export const register = (name, email, password) => {
  return axios.post(`${USER_API_URL}/register`, {
    name,
    email,
    password,
  });
};

export const login = (email, password) => {
  return axios.post(`${USER_API_URL}/login`, {
    email,
    password,
  });
};

// Vendor-related API calls
const VENDOR_API_URL = `${API_BASE_URL}/vendors`;

export const registerVendor = (businessName, email, password, address) => {
  return axios.post(`${VENDOR_API_URL}/register`, {
    businessName,
    email,
    password,
    address,
  });
};

// Job-related API calls
const JOB_API_URL = `${API_BASE_URL}/jobs`;

export const getStoreDetails = (storeCode) => {
  return axios.get(`${API_BASE_URL}/store/${storeCode}`);
};

export const uploadAnonymousJob = (formData) => {
  return axios.post(`${JOB_API_URL}/qr-anonymous-upload`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

export const getJobStatus = (trackingCode) => {
    return axios.get(`${JOB_API_URL}/status/${trackingCode}`);
};
