import { FunctionComponent } from "react";
import { Box, Typography, TextField } from "@mui/material";
import styles from "./SearchBar.module.css";

export type SearchBarType = {
  className?: string;
  value?: string;
  onChange?: (value: string) => void;
};

const SearchBar: FunctionComponent<SearchBarType> = ({
  className = "",
  value = "",
  onChange,
}) => {
  return (
    <section className={[styles.searchBar, className].join(" ")}>
      <Box className={styles.searchBarChild} />
      <Box className={styles.searchContainer}>
        <Typography
          className={styles.h2}
          variant="inherit"
          variantMapping={{ inherit: "h2" }}
          sx={{ fontWeight: "700" }}
        >
          Пошук
        </Typography>
      </Box>
      <TextField
        className={styles.searchBarItem}
        placeholder="Пошук проєктів, людей, команд..."
        variant="outlined"
        value={value}
        onChange={(e) => onChange?.(e.target.value)}
        slotProps={{
          input: {
            startAdornment: (
              <img width="38px" height="33px" src="/group-18.svg" />
            ),
          },
        }}
        sx={{
          "& fieldset": { borderColor: "#6d6c6c" },
          "& .MuiInputBase-root": {
            height: "81px",
            backgroundColor: "#161616",
            paddingLeft: "23px",
            borderRadius: "40px",
            fontSize: "24px",
          },
          "& .MuiInputBase-input": { paddingLeft: "25px", color: "#6d6c6c" },
        }}
      />
    </section>
  );
};

export default SearchBar;